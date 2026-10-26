using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using SpeakingClub.Data.Abstract;
using SpeakingClub.Entity;
using SpeakingClub.Identity;
using SpeakingClub.Models;
using SpeakingClub.Services;

namespace SpeakingClub.Controllers
{
    public class HomeController : Controller
    {
        private static Dictionary<string, DateTime> _contactRateLimit = new();
        private static readonly object _rateLimitLock = new();
        private readonly IMemoryCache _memoryCache;
        private readonly IConfiguration _configuration;
        private readonly ILogger<HomeController> _logger;
        private readonly LanguageService _localization;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEmailSender _emailSender;
        private readonly UserManager<User> _userManager;
        private readonly ExamService _examService;

        public HomeController(
            IConfiguration configuration,
            ILogger<HomeController> logger,
            LanguageService localization,
            IUnitOfWork unitOfWork,
            IMemoryCache memoryCache,
            IEmailSender emailSender,
            UserManager<User> userManager,
            ExamService examService)
        {
            _examService = examService;
            _configuration = configuration;
            _memoryCache = memoryCache;
            _logger = logger;
            _localization = localization;
            _unitOfWork = unitOfWork;
            _emailSender = emailSender;
            _userManager = userManager;
        }

        // Helper: get current lang code
        private string GetLangCode() => CultureInfo.CurrentCulture.TwoLetterISOLanguageName.ToLower();
        private bool IsGerman() => GetLangCode() == "de";
        
        // Helper: get localized URL path based on current culture
        private string GetLocalizedPath(string trPath, string dePath, string fallbackPath)
        {
            var lang = GetLangCode();
            return lang == "de" ? dePath : lang == "tr" ? trPath : fallbackPath;
        }
        
        // Helper: set SEO ViewData
        private void SetSeoData(string title, string description, string keywords, string? canonicalPath = null, string? ogImage = null)
        {
            ViewData["Title"] = title;
            ViewData["Description"] = description;
            ViewData["Keywords"] = keywords;
            if (canonicalPath != null)
                ViewData["Canonical"] = AbsoluteUrl(canonicalPath);
            if (ogImage != null)
                ViewData["OgImage"] = ogImage;
        }

        // Helper: absolute URL on the canonical (www) host
        private static string AbsoluteUrl(string path) => SeoHelper.SiteUrl + path;

        // Helper: hreflang alternates for a page that exists in Turkish and German
        private void SetHreflang(string trPath, string dePath)
        {
            ViewData["HreflangTr"] = AbsoluteUrl(trPath);
            ViewData["HreflangDe"] = AbsoluteUrl(dePath);
        }

        // Helper: 301 any alias of a page (/blog, /about, /Home/Blog, the other language's slug, …) to the one
        // URL the page is canonical at, keeping the query string. Null when the request is already there.
        // Serving the same page under several URLs is what filled Search Console with "alternate page" entries.
        private IActionResult? RedirectToCanonical(string canonicalPath)
        {
            var requestPath = Request.Path.Value ?? "/";
            if (requestPath.Length > 1)
                requestPath = requestPath.TrimEnd('/');
            if (string.Equals(requestPath, canonicalPath, StringComparison.OrdinalIgnoreCase))
                return null;
            return RedirectPermanent(canonicalPath + Request.QueryString);
        }

        private static string FirstNonEmpty(params string?[] values) =>
            values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v)) ?? string.Empty;

        // Helper: image path usable from any page. A bare file name ("cover.jpg") would resolve relative to
        // /yazilar/slug and 404, so site paths always get a leading slash.
        private static string ImagePath(string? image)
        {
            if (string.IsNullOrWhiteSpace(image))
                return string.Empty;
            image = image.Trim();
            return image.StartsWith("/") || image.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                   image.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
                ? image
                : "/" + image;
        }

        // Helper: the URL paths a post exists at. A language has a URL only when the post is translated into it;
        // a post without translations is served from its base fields at the Turkish URL. (SitemapController
        // lists the same URLs.)
        private static (string? TrPath, string? DePath) BlogPaths(SpeakingClub.Entity.Blog blog, string? requestedSlug = null)
        {
            var tr = blog.Translations?.FirstOrDefault(t => t.LanguageCode == "tr");
            var de = blog.Translations?.FirstOrDefault(t => t.LanguageCode == "de");
            var baseSlug = FirstNonEmpty(blog.Slug, blog.Url, requestedSlug);
            var trSlug = tr != null ? FirstNonEmpty(tr.Slug, baseSlug) : de == null ? baseSlug : null;
            var deSlug = de != null ? FirstNonEmpty(de.Slug, baseSlug) : null;
            return (string.IsNullOrEmpty(trSlug) ? null : $"/yazilar/{trSlug}",
                    string.IsNullOrEmpty(deSlug) ? null : $"/beitraege/{deSlug}");
        }

        // Helper: a post as a card in the current language (falls back to the language it exists in)
        private BlogCardModel BuildBlogCard(SpeakingClub.Entity.Blog blog)
        {
            var isGerman = IsGerman();
            var (trPath, dePath) = BlogPaths(blog);
            var translation = blog.Translations?.FirstOrDefault(t => t.LanguageCode == (isGerman ? "de" : "tr"))
                              ?? blog.Translations?.FirstOrDefault();
            var content = FirstNonEmpty(translation?.Content, blog.Content);
            var description = translation != null ? translation.Description : blog.Description;

            return new BlogCardModel
            {
                BlogId = blog.BlogId,
                Title = FirstNonEmpty(translation?.Title, blog.Title),
                Excerpt = !string.IsNullOrWhiteSpace(description)
                    ? HtmlUtility.GetExcerpt(description, 170)
                    : HtmlUtility.GetExcerpt(content, 170),
                Href = (isGerman ? dePath ?? trPath : trPath ?? dePath) ?? "/",
                Image = ImagePath(blog.Image),
                Author = blog.Author ?? string.Empty,
                CategoryName = blog.Category?.Name,
                Date = blog.Date,
                ReadingMinutes = HtmlUtility.ReadingMinutes(content)
            };
        }

        #region SetLanguage
        public async Task<IActionResult> SetLanguage(string culture, string returnUrl)
        {
            // The site has only Turkish and German; anything else (e.g. en-US) falls back to Turkish.
            culture = string.Equals(culture, "de-DE", StringComparison.OrdinalIgnoreCase) ? "de-DE" : "tr-TR";

            Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
                new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1) }
            );

            var targetUrl = await MapUrlToTargetCultureAsync(returnUrl, culture);
            return LocalRedirect(targetUrl);
        }

        private async Task<string> MapUrlToTargetCultureAsync(string currentPath, string targetCulture)
        {
            if (string.IsNullOrEmpty(currentPath)) return "/";

            bool targetIsDe = targetCulture.StartsWith("de", StringComparison.OrdinalIgnoreCase);

            var path = currentPath.Split('?')[0].ToLowerInvariant().TrimEnd('/');
            if (string.IsNullOrEmpty(path)) path = "/";

            // Static page map: any alias → canonical target-culture URL
            var staticMap = new Dictionary<string, (string tr, string de)>(StringComparer.OrdinalIgnoreCase)
            {
                ["/"]            = ("/",            "/de"),
                ["/de"]          = ("/",            "/de"),
                ["/about"]       = ("/hakkimizda",  "/ueber-uns"),
                ["/hakkimizda"]  = ("/hakkimizda",  "/ueber-uns"),
                ["/ueber-uns"]   = ("/hakkimizda",  "/ueber-uns"),
                ["/privacy"]     = ("/gizlilik",    "/datenschutz"),
                ["/gizlilik"]    = ("/gizlilik",    "/datenschutz"),
                ["/datenschutz"] = ("/gizlilik",    "/datenschutz"),
                ["/terms"]               = ("/kullanim-kosullari", "/nutzungsbedingungen"),
                ["/kullanim-kosullari"]  = ("/kullanim-kosullari", "/nutzungsbedingungen"),
                ["/nutzungsbedingungen"] = ("/kullanim-kosullari", "/nutzungsbedingungen"),
                ["/blog"]        = ("/yazilar",     "/beitraege"),
                ["/yazilar"]     = ("/yazilar",     "/beitraege"),
                ["/beitraege"]   = ("/yazilar",     "/beitraege"),
                ["/words"]       = ("/sozluk",      "/woerterbuch"),
                ["/sozluk"]      = ("/sozluk",      "/woerterbuch"),
                ["/woerterbuch"] = ("/sozluk",      "/woerterbuch"),
                ["/quizzes"]     = ("/sinavlar",    "/pruefungen"),
                ["/sinavlar"]    = ("/sinavlar",    "/pruefungen"),
                ["/pruefungen"]  = ("/sinavlar",    "/pruefungen"),
            };

            if (staticMap.TryGetValue(path, out var pair))
                return targetIsDe ? pair.de : pair.tr;

            // Blog detail pages: /yazilar/{slug}, /beitraege/{slug}, /blog/{slug}
            var blogPrefixes = new[] { "/yazilar/", "/beitraege/", "/blog/" };
            foreach (var prefix in blogPrefixes)
            {
                if (path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    var slug = path[prefix.Length..];
                    var targetLang = targetIsDe ? "de" : "tr";
                    var targetPrefix = targetIsDe ? "/beitraege/" : "/yazilar/";

                    // Try slug as-is in target language first
                    var direct = await _unitOfWork.BlogTranslations.GetBySlugAsync(slug, targetLang);
                    if (direct != null)
                        return targetPrefix + direct.Slug;

                    // Slug might belong to source language — find the blog then its target translation
                    var sourceLang = targetIsDe ? "tr" : "de";
                    var source = await _unitOfWork.BlogTranslations.GetBySlugAsync(slug, sourceLang);
                    if (source != null)
                    {
                        var target = await _unitOfWork.BlogTranslations.GetByBlogAndLanguageAsync(source.BlogId, targetLang);
                        if (target != null)
                            return targetPrefix + target.Slug;
                    }

                    // Fallback to blog list
                    return targetIsDe ? "/beitraege" : "/yazilar";
                }
            }

            // Unknown path — go to homepage
            return "/";
        }
        #endregion

        #region Anasayfa
        [HttpGet("")]
        [HttpGet("de")]
        // "Home…" aliases have Order = 1 so generated links (asp-action, RedirectToAction) use "/" and never a redirect
        [HttpGet("Home", Order = 1)]
        [HttpGet("Home/Index", Order = 1)]
        public async Task<IActionResult> Index()
        {
            // The home page has a Turkish (/) and a German (/de) URL. "/" follows the visitor's language: a German
            // visitor is sent on to /de with a temporary redirect (a permanent one would be cached by the browser
            // and keep Turkish visitors away from "/"). Crawlers have no language cookie and get the Turkish page.
            var homePath = IsGerman() ? "/de" : "/";
            if (Request.Path == "/" && homePath == "/de")
                return Redirect("/de" + Request.QueryString);
            if (RedirectToCanonical(homePath) is { } redirect)
                return redirect;

            if (IsGerman())
            {
                SetSeoData(
                    "Deutsch sprechen lernen – online | Almanca Konuş – Suna Koç",
                    "Online-Deutschunterricht mit Muttersprachlerin Suna Koç: Einzel- und Gruppenstunden (4–6 Personen), Einstufungstest und klinisches Deutsch für die Pflege.",
                    "Deutsch sprechen lernen, Online Deutschkurs, Deutschunterricht, Konversation, klinisches Deutsch, Pflegedeutsch",
                    "/de");
            }
            else
            {
                SetSeoData(
                    "Online Almanca Konuşma Dersleri | Almanca Konuş – Suna Koç",
                    "Anadili Almanca Suna Koç ile online birebir ve grup (4–6 kişi) Almanca konuşma dersleri. Ücretsiz seviye testi, Almanca sözlük ve klinik Almanca.",
                    "online almanca dersi, almanca konuşma kursu, almanca özel ders, almanca konuşma pratiği, klinik almanca",
                    "/");
            }
            SetHreflang("/", "/de");

            // Posts marked "show on home" first, then the newest; same cards (translation, URL) as the blog list
            var entityBlogs = await _unitOfWork.Blogs.GetPublishedAsync();
            var model = new IndexModel
            {
                LatestPosts = entityBlogs
                    .Where(b => !b.NoIndex)
                    .OrderByDescending(b => b.isHome)
                    .ThenByDescending(b => b.Date)
                    .Take(3)
                    .Select(BuildBlogCard)
                    .ToList()
            };

            return View(model);
        }
        #endregion

        #region Gizlilik
        [HttpGet("privacy")]
        [HttpGet("gizlilik")]
        [HttpGet("datenschutz")]
        [HttpGet("Home/Privacy", Order = 1)]
        public IActionResult Privacy()
        {
            if (RedirectToCanonical(GetLocalizedPath("/gizlilik", "/datenschutz", "/gizlilik")) is { } redirect)
                return redirect;

            if (IsGerman())
            {
                SetSeoData(
                    "Datenschutzerklärung | Almanca Konuş",
                    "Welche personenbezogenen Daten Almanca Konuş verarbeitet, wofür und welche Rechte Sie nach der DSGVO haben.",
                    "Datenschutz, Privatsphäre, DSGVO",
                    "/datenschutz");
            }
            else
            {
                SetSeoData(
                    "Gizlilik Politikası | Almanca Konuş",
                    "Almanca Konuş hangi kişisel verileri, hangi amaçla işliyor ve KVKK kapsamında hangi haklara sahipsiniz?",
                    "gizlilik, kişisel veri, KVKK",
                    "/gizlilik");
            }
            SetHreflang("/gizlilik", "/datenschutz");
            return View();
        }

        [HttpGet("terms")]
        [HttpGet("kullanim-kosullari")]
        [HttpGet("nutzungsbedingungen")]
        public IActionResult Terms()
        {
            if (RedirectToCanonical(GetLocalizedPath("/kullanim-kosullari", "/nutzungsbedingungen", "/kullanim-kosullari")) is { } redirect)
                return redirect;

            if (IsGerman())
            {
                SetSeoData(
                    "Nutzungsbedingungen | Almanca Konuş",
                    "Regeln für die Nutzung von Almanca Konuş: Konto, Online-Prüfungen, Wörterbuch, Inhalte und Haftung.",
                    "Nutzungsbedingungen, AGB, Regeln",
                    "/nutzungsbedingungen");
            }
            else
            {
                SetSeoData(
                    "Kullanım Koşulları | Almanca Konuş",
                    "Almanca Konuş'u kullanma kuralları: hesap, online sınavlar, sözlük, içerikler ve sorumluluk.",
                    "kullanım koşulları, hizmet şartları, kurallar",
                    "/kullanim-kosullari");
            }
            SetHreflang("/kullanim-kosullari", "/nutzungsbedingungen");
            return View();
        }
        #endregion

        #region Klinik Almanca - Hekimler / Ärzte
        [HttpGet("hekim-almancasi")]
        [HttpGet("aerztedeutsch")]
        public IActionResult DoctorsGerman()
        {
            if (IsGerman())
            {
                SetSeoData(
                    "Deutsch für Ärzte: FSP, Anamnese, Arztbrief | Almanca Konuş",
                    "Klinisches Deutsch für Ärztinnen und Ärzte: FSP-Vorbereitung, Anamnese, Patientenvorstellung, Arztbrief und Redemittel – mit echten Beispieldialogen.",
                    "Deutsch für Ärzte, FSP, Fachsprachprüfung, Anamnese, Arztbrief, Patientenvorstellung, klinische Kommunikation, medizinische Abkürzungen, Redemittel",
                    "/aerztedeutsch");
            }
            else
            {
                SetSeoData(
                    "Hekim Almancası: FSP, Anamnez, Arztbrief | Almanca Konuş",
                    "Doktorlar için klinik Almanca: FSP hazırlığı, anamnez, hasta sunumu (Patientenvorstellung), Arztbrief ve kalıp ifadeler – gerçek örnek diyaloglarla.",
                    "hekimler için almanca, FSP, Fachsprachprüfung, anamnez, Arztbrief, hasta sunumu, klinik iletişim, tıbbi kısaltmalar, Redemittel",
                    "/hekim-almancasi");
            }
            SetHreflang("/hekim-almancasi", "/aerztedeutsch");
            return View();
        }
        #endregion

        #region Klinik Almanca - Hemşireler / Pflegekräfte
        [HttpGet("hemsirelik-almancasi")]
        [HttpGet("pflegedeutsch")]
        public IActionResult NursesGerman()
        {
            if (IsGerman())
            {
                SetSeoData(
                    "Pflegedeutsch: Übergabe & Dokumentation | Almanca Konuş",
                    "Deutsch für Pflegekräfte: Patientenübergabe, Pflegekommunikation, Pflegedokumentation und Redemittel – mit Beispieldialogen aus dem Klinikalltag.",
                    "Pflegedeutsch, Deutsch für Pflegekräfte, Patientenübergabe, Pflegekommunikation, Pflegedokumentation, medizinische Abkürzungen, Redemittel für Pflegekräfte",
                    "/pflegedeutsch");
            }
            else
            {
                SetSeoData(
                    "Hemşirelik Almancası: Hasta Devri, İletişim | Almanca Konuş",
                    "Hemşireler için Almanca: hastayla iletişim, hasta devri (Patientenübergabe), bakım dokümantasyonu ve kalıp ifadeler – klinikten örnek diyaloglarla.",
                    "hemşirelik almancası, hemşireler için almanca, hasta devri, Pflegekommunikation, Pflegedokumentation, tıbbi kısaltmalar, Redemittel",
                    "/hemsirelik-almancasi");
            }
            SetHreflang("/hemsirelik-almancasi", "/pflegedeutsch");
            return View();
        }
        #endregion

        #region Hakkında
        [HttpGet("about")]
        [HttpGet("hakkimizda")]
        [HttpGet("ueber-uns")]
        [HttpGet("Home/About", Order = 1)]
        public IActionResult About()
        {
            if (RedirectToCanonical(GetLocalizedPath("/hakkimizda", "/ueber-uns", "/hakkimizda")) is { } redirect)
                return redirect;

            ViewBag.TurnstileSiteKey = _configuration["Turnstile:SiteKey"];
            ViewBag.CKEditorLicenseKey = _configuration["License:CKEditor"];
            
            if (IsGerman())
            {
                SetSeoData(
                    "Über Suna Koç – Deutsch-Sprechcoach | Almanca Konuş",
                    "Suna Koç ist in Bremen aufgewachsen, Deutsch ist ihre Muttersprache. Seit über 20 Jahren unterrichtet sie, heute online und mit Fokus aufs Sprechen.",
                    "Suna Koç, Deutschlehrerin, Muttersprachlerin, Deutsch sprechen lernen, Online-Deutschunterricht, Kontakt",
                    "/ueber-uns");
            }
            else
            {
                SetSeoData(
                    "Suna Koç Kimdir? Almanca Konuşma Koçu | Almanca Konuş",
                    "Bremen'de büyüyen, Almanca ana dili olan Suna Koç 20 yılı aşkın deneyimiyle online Almanca konuşma dersleri veriyor. Sık sorulan sorular ve iletişim.",
                    "Suna Koç, almanca öğretmeni, anadili almanca öğretmen, almanca konuşma dersi, online almanca, iletişim",
                    "/hakkimizda");
            }
            SetHreflang("/hakkimizda", "/ueber-uns");
            var model = CreateAboutPageViewModel();
            return View(model);
        }
        #endregion

        #region Contact Form İşlemleri
        // POST: /Home/Contact
        [HttpPost]
        public async Task<IActionResult> Contact(AboutPageViewModel model)
        {
            // Bot detection - check honeypot and other fields
            if (!string.IsNullOrEmpty(Request.Form["honeypot"]) ||
                !string.IsNullOrEmpty(Request.Form["phone"]) ||
                !string.IsNullOrEmpty(Request.Form["website"]))
            {
                // Honeypot filled --> Bot!
                _logger.LogWarning("Bot attempt on Contact form from {IP}", HttpContext.Connection.RemoteIpAddress?.ToString());
                ModelState.AddModelError("", "Invalid request.");
                var aboutModel = CreateAboutPageViewModel();
                if (model?.ContactForm != null)
                    aboutModel.ContactForm = model.ContactForm;
                return View("About", aboutModel);
            }
            string userIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            lock (_rateLimitLock)
            {
                if (_contactRateLimit.TryGetValue(userIp, out var lastSubmit))
                {
                    if (DateTime.UtcNow - lastSubmit < TimeSpan.FromSeconds(30))
                    {
                        ModelState.AddModelError("", "Çok sık deneme yaptınız. Lütfen biraz bekleyin.");
                        var aboutModel = CreateAboutPageViewModel();
                        if (model?.ContactForm != null)
                            aboutModel.ContactForm = model.ContactForm;
                        return View("About", aboutModel);

                    }
                }
                _contactRateLimit[userIp] = DateTime.UtcNow;
            }

            var ip = HttpContext.Connection.RemoteIpAddress != null
                ? HttpContext.Connection.RemoteIpAddress.ToString()
                : "unknown";
            var cacheKey = $"LoginAttempts:{ip}";
            int count = _memoryCache.GetOrCreate(cacheKey, entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(2);
                return 0;
            });
            if (count >= 5)
            {
                ModelState.AddModelError("", "Çok fazla deneme yaptınız. Lütfen 2 dakika sonra tekrar deneyin.");
                var aboutModel = CreateAboutPageViewModel();
                if (model?.ContactForm != null)
                    aboutModel.ContactForm = model.ContactForm;
                return View("About", aboutModel);

            }
            _memoryCache.Set(cacheKey, count + 1, TimeSpan.FromMinutes(2));
            string? recaptchaResponse = Request.Form["cf-turnstile-response"];
            if (!await RecaptchaIsValid(recaptchaResponse ?? ""))
            {
                ModelState.AddModelError("", "Lütfen robot olmadığınızı doğrulayın (reCAPTCHA).");
                var aboutModel = CreateAboutPageViewModel();
                if (model?.ContactForm != null)
                    aboutModel.ContactForm = model.ContactForm;
                return View("About", aboutModel);

            }
            if (!ModelState.IsValid)
            {
                // If validation fails, return the About view with the current model data.
                var aboutModel = CreateAboutPageViewModel();
                if (model?.ContactForm != null)
                    aboutModel.ContactForm = model.ContactForm;
                return View("About", aboutModel);

            }

            // Access the ContactForm properties from the model.
            var contact = model.ContactForm;
            var name = contact.Name;
            var email = contact.Email;
            var message = contact.Message;

            // Administrator's email address.
            string adminEmail = "almancakonus.info@gmail.com";

            // Define file paths for your templates.
            // Adjust the paths if your EmailTemplates folder is in a different location.
            string adminTemplatePath = Path.Combine(Directory.GetCurrentDirectory(), "EmailTemplates", "AdminNotification.html");
            // Determine the current culture (for example,   "de-DE", or "tr-TR")
            var currentCulture = CultureInfo.CurrentCulture.Name; // or use RequestCulture if available

            string userTemplatePath;
            if (currentCulture.StartsWith("de", StringComparison.OrdinalIgnoreCase))
            {
                userTemplatePath = Path.Combine(Directory.GetCurrentDirectory(), "EmailTemplates", "UserNotification_de.html");
            }
            else
            {
                userTemplatePath = Path.Combine(Directory.GetCurrentDirectory(), "EmailTemplates", "UserNotification_tr.html");
            }

            // Read the HTML template content from files.
            string adminEmailBodyTemplate = await System.IO.File.ReadAllTextAsync(adminTemplatePath);
            string userEmailBodyTemplate = await System.IO.File.ReadAllTextAsync(userTemplatePath);

            // Strip HTML tags from message (remove CKEditor HTML) and escape for security
            string plainMessage = StripHtmlTags(message);

            // Replace placeholders in the admin template.
            string adminEmailBody = adminEmailBodyTemplate
                .Replace("{UserName}", System.Net.WebUtility.HtmlEncode(name))
                .Replace("{UserEmail}", System.Net.WebUtility.HtmlEncode(email))
                .Replace("{UserMessage}", System.Net.WebUtility.HtmlEncode(plainMessage));

            // For the user email template, you might not need to replace placeholders if it’s static.
            string userEmailBody = userEmailBodyTemplate;

            // Set subjects (you can hardcode these or load from configuration)
            string adminSubject = "New Contact Message Received";
            string userSubject = "Thank You for Your Message";

            try
            {
                // Send email to the administrator.
                await _emailSender.SendEmailAsync(adminEmail, adminSubject, adminEmailBody);
                // Send automatic response email to the user.
                await _emailSender.SendEmailAsync(email, userSubject, userEmailBody);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Contact form email sending failed.");
                TempData["ContactError"] = "An error occurred while sending your message. Please try again later.";
                return Redirect(GetLocalizedPath("/hakkimizda", "/ueber-uns", "/hakkimizda"));
            }

            TempData["ContactSuccess"] = "Your message was sent successfully!";
            return Redirect(GetLocalizedPath("/hakkimizda", "/ueber-uns", "/hakkimizda"));
        }

        // Contact form gönderimi sonrası onay sayfası
        public IActionResult ContactConfirmation()
        {
            return View();
        }

        // Hakkında sayfası için model oluşturma (ContactForm alanını da içerir)
        private AboutPageViewModel CreateAboutPageViewModel()
        {
            var model = new AboutPageViewModel
            {
                PageTitle = _localization.GetKey("PageTitle").Value,
                HeroSection = new HeroSectionModel
                {
                    Greeting = _localization.GetKey("HeroGreeting").Value,
                    Subtitle = _localization.GetKey("HeroSubtitle").Value,
                    ImageAlt = _localization.GetKey("HeroImageAlt").Value,
                },
                AboutSection = new AboutSectionModel
                {
                    Title = _localization.GetKey("AboutSectionTitle").Value,
                    Paragraph1 = _localization.GetKey("AboutParagraph1").Value,
                    Paragraph2 = _localization.GetKey("AboutParagraph2").Value,
                    Paragraph3 = _localization.GetKey("AboutParagraph3").Value,
                    Paragraph4 = _localization.GetKey("AboutParagraph4").Value,
                    Paragraph5 = _localization.GetKey("AboutParagraph5").Value,
                    ImageAlt = _localization.GetKey("AboutImageAlt").Value
                },
                ProcessSection = new ProcessSectionModel
                {
                    Title = _localization.GetKey("ProcessSectionTitle").Value,
                    Steps = new System.Collections.Generic.List<ProcessStepModel>
                    {
                        new ProcessStepModel
                        {
                            Id = 1,
                            Title = _localization.GetKey("Step1_Title").Value,
                            Description = _localization.GetKey("Step1_Description").Value,
                            LinkText = _localization.GetKey("Step1_Link").Value,
                            IconClass = "fa-regular fa-clipboard"
                        },
                        new ProcessStepModel
                        {
                            Id = 2,
                            Title = _localization.GetKey("Step2_Title").Value,
                            Description = _localization.GetKey("Step2_Description").Value,
                            LinkText = _localization.GetKey("Step2_Link").Value,
                            IconClass ="fa-regular fa-hand-point-up"
                        },
                        new ProcessStepModel
                        {
                            Id = 3,
                            Title = _localization.GetKey("Step3_Title").Value,
                            Description = _localization.GetKey("Step3_Description").Value,
                            LinkText = _localization.GetKey("Step3_Link").Value,
                            IconClass = "fa-regular fa-handshake"
                        },
                        new ProcessStepModel
                        {
                            Id = 4,
                            Title = _localization.GetKey("Step4_Title").Value,
                            Description = _localization.GetKey("Step4_Description").Value,
                            LinkText = _localization.GetKey("Step4_Link").Value,
                            IconClass = "fa-regular fa-circle-question"
                        },
                        new ProcessStepModel
                        {
                            Id = 5,
                            Title = _localization.GetKey("Step5_Title").Value,
                            Description = _localization.GetKey("Step5_Description").Value,
                            LinkText = _localization.GetKey("Step5_Link").Value,
                            IconClass = "fa-solid fa-list-check"
                        }
                    }
                },
                FAQSection = new FAQSectionModel
                {
                    Title = _localization.GetKey("FAQSectionTitle").Value,
                    FAQItems = new System.Collections.Generic.List<FAQItemModel>
                    {
                        new FAQItemModel
                        {
                            Question = _localization.GetKey("FAQ1_Question").Value,
                            Answer = _localization.GetKey("FAQ1_Answer").Value
                        },
                        new FAQItemModel
                        {
                            Question = _localization.GetKey("FAQ2_Question").Value,
                            Answer = _localization.GetKey("FAQ2_Answer").Value
                        },
                        new FAQItemModel
                        {
                            Question = _localization.GetKey("FAQ3_Question").Value,
                            Answer = _localization.GetKey("FAQ3_Answer").Value
                        }
                    }
                },
                ContactSection = new ContactSectionModel
                {
                    Title = _localization.GetKey("ContactSectionTitle").Value,
                    NameLabel = _localization.GetKey("Contact_NameLabel").Value,
                    EmailLabel = _localization.GetKey("Contact_EmailLabel").Value,
                    MessageLabel = _localization.GetKey("Contact_MessageLabel").Value,
                    MessagePlaceholder = _localization.GetKey("Contact_MessagePlaceholder").Value,
                    ButtonText = _localization.GetKey("Contact_ButtonText").Value
                },
                // ContactForm alanını boş olarak oluşturuyoruz.
                ContactForm = new ContactFormViewModel()
            };

            return model;
        }
        #endregion

        #region Blog
        [HttpGet("blog")]
        [HttpGet("yazilar")]
        [HttpGet("beitraege")]
        [HttpGet("Home/Blog", Order = 1)]
        public async Task<IActionResult> Blog(string category, string tag, string searchTerm, int page = 1)
        {
            var isGerman = IsGerman();
            var blogListPath = isGerman ? "/beitraege" : "/yazilar";
            if (RedirectToCanonical(blogListPath) is { } redirect)
                return redirect;

            // Older list pages linked "All Categories" as a category filter
            if (category == "All Categories")
                category = null!;
            if (page < 1)
                page = 1;

            // SEO metadata for blog listing
            if (isGerman)
            {
                SetSeoData(
                    "Blog: Deutsch lernen & klinisches Deutsch | Almanca Konuş",
                    "Tipps zum Deutschsprechen, Grammatik und klinisches Deutsch für Pflege und Ärzte: Artikel von Suna Koç auf Deutsch und Türkisch.",
                    "Blog, Deutsch lernen, Artikel, Grammatik, Vokabeln, Tipps, Deutschkurs",
                    "/beitraege");
            }
            else
            {
                SetSeoData(
                    "Blog: Almanca Konuşma ve Klinik Almanca | Almanca Konuş",
                    "Almanca konuşma ipuçları, gramer ve sağlık çalışanları için klinik Almanca: Suna Koç'un Türkçe ve Almanca yazıları.",
                    "blog, almanca öğren, yazılar, gramer, kelime, ipuçları, almanca kursu",
                    "/yazilar");
            }

            var entityBlogs = (await _unitOfWork.Blogs.GetPublishedAsync()).ToList();
            IEnumerable<SpeakingClub.Entity.Blog> blogsQuery = entityBlogs;

            // Filter by category if provided.
            if (!string.IsNullOrWhiteSpace(category))
            {
                blogsQuery = blogsQuery.Where(b => b.Category != null && string.Equals(b.Category.Name, category, StringComparison.OrdinalIgnoreCase));
            }

            // Filter by tag if provided.
            if (!string.IsNullOrWhiteSpace(tag))
            {
                blogsQuery = blogsQuery.Where(b => b.Tags.Any(t => string.Equals(t.Name, tag, StringComparison.OrdinalIgnoreCase)));
            }

            // Search the title, description and text of the post in every language (case-insensitive).
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim();
                bool Has(string? text) => !string.IsNullOrEmpty(text) && text.Contains(term, StringComparison.CurrentCultureIgnoreCase);
                blogsQuery = blogsQuery.Where(b =>
                    Has(b.Title) || Has(b.Description) || Has(HtmlUtility.StripHtml(b.Content)) ||
                    (b.Translations?.Any(t => Has(t.Title) || Has(t.Description) || Has(HtmlUtility.StripHtml(t.Content))) ?? false));
            }

            var matching = blogsQuery.OrderByDescending(b => b.Date).ToList();

            // Paging logic.
            int pageSize = 9;
            int totalBlogs = matching.Count;
            int totalPages = (int)Math.Ceiling(totalBlogs / (double)pageSize);
            if (page > 1 && page > totalPages)
                return NotFound();

            var posts = matching
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(BuildBlogCard)
                .ToList();

            var model = new BlogFilterViewModel
            {
                Category = category,
                Tag = tag,
                SearchTerm = searchTerm,
                CurrentPage = page,
                TotalPages = totalPages,
                TotalBlogs = totalBlogs,
                Posts = posts,
                BlogListPath = blogListPath,
                // Only categories and tags that have published posts
                Categories = entityBlogs
                    .Where(b => b.Category != null)
                    .Select(b => b.Category.Name)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(c => c)
                    .ToList(),
                AvailableTags = entityBlogs
                    .SelectMany(b => b.Tags.Select(t => t.Name))
                    .GroupBy(t => t, StringComparer.OrdinalIgnoreCase)
                    .OrderByDescending(g => g.Count())
                    .ThenBy(g => g.Key)
                    .Select(g => g.Key)
                    .ToList(),

                // UI text labels
                BlogList_Title = _localization.GetKey("Blog").Value,
                BlogList_Description = _localization.GetKey("BlogDesc").Value,
                BlogList_SearchLabel = _localization.GetKey("Search").Value,
                BlogList_AllCategories = _localization.GetKey("CategoryLabel").Value,
                BlogList_TagLabel = _localization.GetKey("Tag").Value,
                BlogList_ReadMore = _localization.GetKey("ReadMore").Value,
            };

            // Filtered and searched lists are not indexed (endless URL variants of the same posts), but their links
            // are followed. Page 2+ is its own canonical so Google keeps reaching the older posts.
            var pageQuery = page > 1 ? $"?page={page}" : string.Empty;
            if (model.IsFiltered)
            {
                ViewData["Robots"] = "noindex, follow";
            }
            else if (page > 1)
            {
                ViewData["Canonical"] = AbsoluteUrl(blogListPath + pageQuery);
                ViewData["Title"] = $"{ViewData["Title"]} – {(isGerman ? "Seite" : "Sayfa")} {page}";
            }
            SetHreflang("/yazilar" + pageQuery, "/beitraege" + pageQuery);

            return View(model);
        }

        [HttpGet("blog/{url}")]
        [HttpGet("yazilar/{url}")]
        [HttpGet("beitraege/{url}")]
        [HttpGet("Home/BlogDetail/{url}", Order = 1)]
        public async Task<IActionResult> BlogDetail(string url)
        {
            try
            {
                _logger.LogInformation("Fetching blog detail for url {url}.", url);

                // Search by base Url, Slug, or translated Slug
                var blog = await _unitOfWork.Blogs.GetByUrlAsync(url);

                if (blog == null)
                {
                    _logger.LogWarning("Blog with slug {url} not found.", url);
                    return NotFound();
                }

                var canPreview = User.IsInRole("Root") || User.IsInRole("Admin") || User.IsInRole("Teacher");
                if (!blog.IsPublished && !canPreview)
                {
                    return NotFound();
                }

                var isGerman = IsGerman();
                var langCode = isGerman ? "de" : "tr";

                // One URL per language version. /blog/x, /Home/BlogDetail/x, the other language's slug and a
                // language the post isn't translated into all 301 to it.
                var (trPath, dePath) = BlogPaths(blog, url);
                var canonicalPath = (isGerman ? dePath : trPath) ?? trPath ?? dePath!;
                if (RedirectToCanonical(canonicalPath) is { } redirect)
                    return redirect;

                // Read the translation into locals. The entity itself is tracked and saved below (view count),
                // so assigning the translated text to it would overwrite the post's base fields.
                var translation = blog.Translations?.FirstOrDefault(t => t.LanguageCode == langCode);
                var title = FirstNonEmpty(translation?.Title, blog.Title);
                var rawContent = FirstNonEmpty(translation?.Content, blog.Content);
                var lead = translation != null ? translation.Description : blog.Description;

                var metaTitle = translation != null
                    ? FirstNonEmpty(translation.MetaTitle, translation.Title, blog.Title)
                    : FirstNonEmpty(blog.MetaTitle, blog.Title);
                var metaDesc = translation != null
                    ? FirstNonEmpty(translation.MetaDescription, translation.Description)
                    : FirstNonEmpty(blog.MetaDescription, blog.Description);
                if (string.IsNullOrWhiteSpace(metaDesc))
                    metaDesc = HtmlUtility.GetExcerpt(rawContent, 160);
                var tagNames = blog.Tags?.Select(t => t.Name).ToList() ?? new List<string>();
                var metaKeywords = FirstNonEmpty(translation != null ? translation.MetaKeywords : blog.MetaKeywords, string.Join(", ", tagNames));

                // Brand suffix unless the editor already wrote a full "… | …" title
                var pageTitle = metaTitle.Contains('|') ? metaTitle : $"{metaTitle} | Almanca Konuş";
                SetSeoData(pageTitle, metaDesc, metaKeywords, canonicalPath);
                ViewData["OgTitle"] = FirstNonEmpty(translation != null ? translation.OgTitle : blog.OgTitle, metaTitle);
                ViewData["OgDescription"] = FirstNonEmpty(translation != null ? translation.OgDescription : blog.OgDescription, metaDesc);
                ViewData["OgType"] = "article";
                var image = ImagePath(blog.Image);
                var imageUrl = image.StartsWith("/") ? AbsoluteUrl(image) : FirstNonEmpty(image, AbsoluteUrl("/img/header_logo.png"));
                if (!string.IsNullOrEmpty(image))
                    ViewData["OgImage"] = imageUrl;

                if (!blog.IsPublished)
                    ViewData["Robots"] = "noindex, nofollow";
                else if (blog.NoIndex || blog.NoFollow)
                    ViewData["Robots"] = $"{(blog.NoIndex ? "noindex" : "index")}, {(blog.NoFollow ? "nofollow" : "follow")}";

                // Hreflang only when the post exists in both languages
                if (trPath != null && dePath != null)
                    SetHreflang(trPath, dePath);

                var tableOfContents = new List<TocItem>();
                var content = HtmlUtility.PrepareArticleHtml(ConvertHttpToHttps(rawContent), tableOfContents);
                var canonicalUrl = AbsoluteUrl(canonicalPath);
                var blogListPath = isGerman ? "/beitraege" : "/yazilar";
                var datePublished = blog.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                var lastModified = new[] { blog.LastModified, translation?.LastModified }.Max();
                var dateModified = lastModified.HasValue && lastModified.Value > blog.Date
                    ? lastModified.Value.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture)
                    : datePublished;

                // BlogPosting + BreadcrumbList structured data. Serialized (not concatenated) so quotes, line
                // breaks and "</script>" in a title cannot break the JSON or the page.
                var schema = new Dictionary<string, object?>
                {
                    ["@context"] = "https://schema.org",
                    ["@graph"] = new object[]
                    {
                        new Dictionary<string, object?>
                        {
                            ["@type"] = "BlogPosting",
                            ["@id"] = canonicalUrl + "#article",
                            ["mainEntityOfPage"] = canonicalUrl,
                            ["url"] = canonicalUrl,
                            ["headline"] = title.Length > 110 ? title[..110] : title,
                            ["description"] = metaDesc,
                            ["image"] = imageUrl,
                            ["datePublished"] = datePublished,
                            ["dateModified"] = dateModified,
                            ["inLanguage"] = langCode,
                            ["wordCount"] = HtmlUtility.StripHtml(rawContent).Split(' ', StringSplitOptions.RemoveEmptyEntries).Length,
                            ["articleSection"] = blog.Category?.Name,
                            ["keywords"] = tagNames.Count > 0 ? string.Join(", ", tagNames) : null,
                            ["author"] = new Dictionary<string, object?>
                            {
                                ["@type"] = "Person",
                                ["name"] = FirstNonEmpty(blog.Author, "Suna Koç"),
                                ["url"] = AbsoluteUrl(isGerman ? "/ueber-uns" : "/hakkimizda")
                            },
                            // The organization itself is described once, in _Layout (same @id)
                            ["publisher"] = new Dictionary<string, object?>
                            {
                                ["@type"] = "EducationalOrganization",
                                ["@id"] = SeoHelper.SiteUrl + "/#organization",
                                ["name"] = "Almanca Konuş",
                                ["url"] = SeoHelper.SiteUrl + "/",
                                ["logo"] = new Dictionary<string, object?>
                                {
                                    ["@type"] = "ImageObject",
                                    ["url"] = AbsoluteUrl("/img/logo-512.png"),
                                    ["width"] = 512,
                                    ["height"] = 512
                                }
                            }
                        },
                        new Dictionary<string, object?>
                        {
                            ["@type"] = "BreadcrumbList",
                            ["itemListElement"] = new object[]
                            {
                                BreadcrumbItem(1, isGerman ? "Startseite" : "Ana Sayfa", SeoHelper.SiteUrl + (isGerman ? "/de" : "/")),
                                BreadcrumbItem(2, "Blog", AbsoluteUrl(blogListPath)),
                                BreadcrumbItem(3, title, canonicalUrl)
                            }
                        }
                    }
                };
                var schemaJson = System.Text.Json.JsonSerializer.Serialize(schema, new System.Text.Json.JsonSerializerOptions
                {
                    // Keeps ç, ş, ü readable while still escaping <, >, & and quotes
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.Create(System.Text.Unicode.UnicodeRanges.All),
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                });
                ViewData["ArticleSchema"] = $"<script type=\"application/ld+json\">{schemaJson}</script>";

                // Increment view count and persist
                try
                {
                    blog.ViewCount += 1;
                    await _unitOfWork.SaveAsync();
                }
                catch (Exception saveEx)
                {
                    _logger.LogWarning(saveEx, "Failed to increment view count for blog {BlogId}", blog.BlogId);
                }

                // Read-next: posts sharing tags (and the category) first, then the newest. Internal links like these
                // are how Google finds and values the older posts.
                var tagIds = blog.Tags?.Select(t => t.TagId).ToHashSet() ?? new HashSet<int>();
                var relatedPosts = (await _unitOfWork.Blogs.GetPublishedAsync())
                    .Where(b => b.BlogId != blog.BlogId && !b.NoIndex)
                    .Select(b => new
                    {
                        Blog = b,
                        Score = b.Tags.Count(t => tagIds.Contains(t.TagId)) * 2
                                + (blog.CategoryId != null && b.CategoryId == blog.CategoryId ? 1 : 0)
                    })
                    .OrderByDescending(x => x.Score)
                    .ThenByDescending(x => x.Blog.Date)
                    .Take(3)
                    .Select(x => BuildBlogCard(x.Blog))
                    .ToList();

                // Create the view model.
                var viewModel = new BlogDetailViewModel
                {
                    BlogId = blog.BlogId,
                    Title = title,
                    Description = lead ?? string.Empty,
                    Content = content,
                    Url = canonicalPath,
                    CanonicalUrl = canonicalUrl,
                    BlogListPath = blogListPath,
                    Date = blog.Date,
                    LastModified = lastModified,
                    RawMaps = blog.RawMaps,
                    RawYT = blog.RawYT,
                    Author = blog.Author ?? string.Empty,
                    Image = image,
                    CategoryId = blog.CategoryId,
                    Category = blog.Category,
                    Tags = blog.Tags?.ToList() ?? new List<Tag>(),
                    Quizzes = blog.Quiz,
                    ViewCount = blog.ViewCount,
                    ReadingMinutes = HtmlUtility.ReadingMinutes(rawContent),
                    ContentLanguage = langCode,
                    TableOfContents = tableOfContents,
                    Files = blog.Files?.OrderBy(f => f.SortOrder).ToList() ?? new List<BlogFile>(),
                    RelatedPosts = relatedPosts
                };

                // Load the selected question directly by its stored ID (includes answers).
                if (blog.SelectedQuestionId.HasValue)
                {
                    viewModel.QuizQuestion = await _unitOfWork.Questions.GetByIdAsync(blog.SelectedQuestionId.Value);
                }

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving blog detail for url {url}.", url);
                TempData["ErrorMessage"] = "An error occurred while retrieving the blog post. Please try again later.";
                return RedirectToAction("Index");
            }
        }

        private static Dictionary<string, object?> BreadcrumbItem(int position, string name, string item) => new()
        {
            ["@type"] = "ListItem",
            ["position"] = position,
            ["name"] = name,
            ["item"] = item
        };

        [HttpGet("download/{fileId:int}")]
        public async Task<IActionResult> DownloadBlogFile(int fileId)
        {
            var blogFile = await _unitOfWork.BlogFiles.GetByIdAsync(fileId);
            if (blogFile == null)
                return NotFound();

            // If not authenticated, redirect to login and return to the blog page
            // (not the download URL) so the browser can handle the file download cleanly
            if (!User.Identity!.IsAuthenticated)
            {
                var blog = await _unitOfWork.Blogs.GetByIdAsync(blogFile.BlogId);
                var blogSlug = blog?.Slug ?? string.Empty;
                var returnPath = string.IsNullOrEmpty(blogSlug)
                    ? "/"
                    : $"/blog/{blogSlug}?autoDownload={fileId}";
                return Redirect($"/Account/Login?ReturnUrl={Uri.EscapeDataString(returnPath)}");
            }

            var filePath = Path.Combine(
                ((IWebHostEnvironment)HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>()).WebRootPath,
                blogFile.StoredFilePath.TrimStart('/'));

            if (!System.IO.File.Exists(filePath))
                return NotFound();

            // Increment download count
            blogFile.DownloadCount++;
            _unitOfWork.BlogFiles.Update(blogFile);

            // Log the download
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                var log = new DownloadLog
                {
                    UserId = user.Id,
                    UserEmail = user.Email ?? string.Empty,
                    UserFullName = $"{user.FirstName} {user.LastName}".Trim(),
                    BlogFileId = blogFile.FileId,
                    FileName = blogFile.OriginalFileName,
                    DownloadedAt = DateTime.UtcNow
                };
                await _unitOfWork.GenericRepository<DownloadLog>().AddAsync(log);
            }

            await _unitOfWork.SaveAsync();

            var contentType = blogFile.ContentType ?? "application/octet-stream";
            return PhysicalFile(filePath, contentType, blogFile.OriginalFileName);
        }

        [HttpGet("api/download-count/{fileId:int}")]
        public async Task<IActionResult> GetDownloadCount(int fileId)
        {
            var blogFile = await _unitOfWork.BlogFiles.GetByIdAsync(fileId);
            if (blogFile == null)
                return NotFound();

            return Json(new { downloadCount = blogFile.DownloadCount });
        }

        #endregion

        #region Sözlük
        [HttpGet("words")]
        [HttpGet("sozluk")]
        [HttpGet("woerterbuch")]
        [HttpGet("Home/Words", Order = 1)]
        public async Task<IActionResult> Words(string? searchTerm, [FromServices] DictionaryLookupService dictionary)
        {
            if (RedirectToCanonical(GetLocalizedPath("/sozluk", "/woerterbuch", "/sozluk")) is { } redirect)
                return redirect;

            var de = IsGerman();
            string T(string tr, string deText) => de ? deText : tr;

            if (de)
            {
                SetSeoData(
                    "Wörterbuch Deutsch–Türkisch | Almanca Konuş",
                    "Deutsch–Türkisch Wörterbuch: unsere eigenen Einträge mit Beispielen, dazu Übersetzungen von DeepL und PONS.",
                    "Wörterbuch, Deutsch, Türkisch, Übersetzung, Bedeutung, Aussprache",
                    "/woerterbuch");
            }
            else
            {
                SetSeoData(
                    "Almanca–Türkçe Sözlük | Almanca Konuş",
                    "Almanca–Türkçe sözlük: hazırladığımız kelimeler anlamı, örnek cümlesi ve telaffuzuyla. Bulunamayan kelimeler için DeepL ve PONS çevirisi.",
                    "sözlük, almanca, türkçe, çeviri, anlam, telaffuz",
                    "/sozluk");
            }
            SetHreflang("/sozluk", "/woerterbuch");

            var model = new WordViewModel { SearchTerm = Data.Concrete.WordRepository.NormalizeTerm(searchTerm) };

            if (model.SearchTerm.Length == 0)
            {
                model.Suggestions = await dictionary.SuggestionsAsync(10);
                return View(model);
            }

            // Search result pages are not worth indexing; the empty page is
            ViewData["Robots"] = "noindex, follow";

            if (model.SearchTerm.Length > DictionaryLookupService.MaxTermLength)
            {
                model.WarningMessage = T(
                    $"Sözlük tek kelime ya da kısa ifadeler içindir (en fazla {DictionaryLookupService.MaxTermLength} karakter).",
                    $"Das Wörterbuch ist für einzelne Wörter oder kurze Ausdrücke (höchstens {DictionaryLookupService.MaxTermLength} Zeichen).");
                return View(model);
            }

            // At most 15 searches per minute and visitor (protects the DeepL / PONS quotas)
            var key = $"WordsSearch:{HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown"}";
            var count = _memoryCache.GetOrCreate(key, entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1);
                return 0;
            });
            if (count >= 15)
            {
                Response.StatusCode = StatusCodes.Status429TooManyRequests;
                model.WarningMessage = T(
                    "Kısa sürede çok fazla arama yaptınız. Lütfen bir dakika bekleyip tekrar deneyin.",
                    "Sie haben in kurzer Zeit sehr viele Suchen gestartet. Bitte warten Sie eine Minute und versuchen Sie es erneut.");
                return View(model);
            }
            _memoryCache.Set(key, count + 1, TimeSpan.FromMinutes(1));

            await dictionary.LookupAsync(model, GetLangCode(), HttpContext.RequestAborted);
            return View(model);
        }

        #endregion

        #region Quizzes (Public)
        [HttpGet("quizzes")]
        [HttpGet("quizzes/{level}")]
        [HttpGet("sinavlar")]
        [HttpGet("sinavlar/{level}")]
        [HttpGet("pruefungen")]
        [HttpGet("pruefungen/{level}")]
        public async Task<IActionResult> Quizzes(string? level)
        {
            var levelPath = level != null ? $"/{level.ToLower()}" : "";
            if (RedirectToCanonical(GetLocalizedPath("/sinavlar", "/pruefungen", "/sinavlar") + levelPath) is { } redirect)
                return redirect;

            // SEO metadata for quizzes. /sinavlar/a1 shows the same list with the A1 filter pre-selected,
            // so its canonical is the list itself.
            var levelText = !string.IsNullOrEmpty(level) ? $" - {level.ToUpperInvariant()}" : "";
            if (IsGerman())
            {
                SetSeoData(
                    $"Deutsch-Einstufungstest online{levelText} | Almanca Konuş",
                    "Testen Sie Ihr Deutsch mit zeitgesteuerten Online-Prüfungen von A1 bis C1 – mit sofortiger Auswertung und Fragen-Review.",
                    "Deutsch-Quiz, Prüfung, Deutschtest, Einstufungstest, A1, A2, B1, B2, C1, Grammatik-Quiz, Vokabel-Quiz",
                    "/pruefungen");
            }
            else
            {
                SetSeoData(
                    $"Almanca Seviye Testi{levelText} | Almanca Konuş",
                    "A1'den C1'e süreli online Almanca seviye testleriyle seviyenizi ölçün; anında puan, doğru cevaplar ve soru soru değerlendirme.",
                    "almanca quiz, sınav, almanca test, seviye tespit sınavı, A1, A2, B1, B2, C1, gramer quiz, kelime quiz",
                    "/sinavlar");
            }
            SetHreflang("/sinavlar", "/pruefungen");

            ViewData["ArticleSchema"] = $@"<script type=""application/ld+json"">
            {{
                ""@context"": ""https://schema.org"",
                ""@type"": ""ItemList"",
                ""name"": ""{(IsGerman() ? "Deutsch-Einstufungstests" : "Almanca Seviye Sınavları")}"",
                ""description"": ""{(IsGerman() ? "Interaktive Deutsch-Prüfungen" : "Almanca interaktif sınavlar")}"",
                ""url"": ""{AbsoluteUrl(IsGerman() ? "/pruefungen" : "/sinavlar")}""
            }}
            </script>";

            var allQuizzes = (await _unitOfWork.Quizzes.GetAllAsync()).ToList();

            // The signed-in user's attempts and running exams
            var user = await _userManager.GetUserAsync(User);
            var userSubmissions = new List<QuizSubmission>();
            var activeAttempts = new List<QuizAttempt>();
            if (user != null)
            {
                await _examService.FinalizeExpiredAttemptsAsync(user); // exams left running past their deadline
                userSubmissions = (await _unitOfWork.QuizSubmissions.GetSubmissionsByUserIdAsync(user.Id)).ToList();
                var now = DateTime.UtcNow;
                activeAttempts = (await _unitOfWork.GenericRepository<QuizAttempt>()
                        .GetAllAsync(a => a.UserId == user.Id && a.SubmittedAt == null))
                    .Where(a => a.ExpiresAt.AddSeconds(ExamRules.GraceSeconds) > now)
                    .ToList();
            }

            // Pre-fetch teacher display names
            var teacherDisplayNames = new Dictionary<string, string>();
            foreach (var teacherUserName in allQuizzes.Select(q => q.TeacherName).Where(t => !string.IsNullOrEmpty(t)).Distinct())
            {
                var teacher = await _userManager.FindByNameAsync(teacherUserName);
                var fullName = teacher == null ? "" : $"{teacher.FirstName} {teacher.LastName}".Trim();
                teacherDisplayNames[teacherUserName] = string.IsNullOrEmpty(fullName) ? teacherUserName : fullName;
            }

            QuizSummaryViewModel MapQuiz(SpeakingClub.Entity.Quiz q)
            {
                var attempts = userSubmissions.Where(s => s.QuizId == q.Id).OrderByDescending(s => s.SubmissionDate).ToList();
                return new QuizSummaryViewModel
                {
                    QuizId = q.Id,
                    QuizTitle = IsGerman() && !string.IsNullOrWhiteSpace(q.TitleDe) ? q.TitleDe : q.Title,
                    QuizDescription = IsGerman() && !string.IsNullOrWhiteSpace(q.DescriptionDe) ? q.DescriptionDe : q.Description,
                    ImageUrl = q.ImageUrl,
                    TeacherName = !string.IsNullOrEmpty(q.TeacherName) && teacherDisplayNames.TryGetValue(q.TeacherName, out var name)
                        ? name
                        : "Almanca Konuş",
                    CategoryName = q.Category?.Name,
                    Tags = q.Tags?.ToList() ?? new List<Tag>(),
                    QuestionCount = q.Questions?.Count ?? 0,
                    TimeLimitMinutes = ExamRules.TimeLimitSeconds(q) / 60,
                    Level = ExamRules.LevelOf(q),
                    AttemptCount = attempts.Count,
                    LastScore = attempts.FirstOrDefault()?.Score,
                    LastAttemptDate = attempts.FirstOrDefault()?.SubmissionDate,
                    BestScore = attempts.Count > 0 ? attempts.Max(s => s.Score) : null,
                    ActiveAttemptId = activeAttempts.Where(a => a.QuizId == q.Id).OrderByDescending(a => a.StartedAt).FirstOrDefault()?.Id
                };
            }

            var viewModel = new CombinedQuizzesViewModel
            {
                AvailableQuizzes = allQuizzes.Select(MapQuiz).ToList(),
                SelectedLevel = string.IsNullOrWhiteSpace(level) ? null : level.ToUpperInvariant(),
                TotalAttempts = userSubmissions.Count,
                AverageScore = userSubmissions.Count > 0 ? (int)Math.Round(userSubmissions.Average(s => s.Score)) : 0,
                BestScore = userSubmissions.Count > 0 ? userSubmissions.Max(s => s.Score) : 0,
                CompletedQuizzes = userSubmissions.Select(s => s.QuizId).Distinct().Count()
            };

            return View("Quizzes", viewModel);
        }
        #endregion

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
        
        private async Task<bool> RecaptchaIsValid(string recaptchaResponse)
        {
            var secret = _configuration["Turnstile:SecretKey"];
            var remoteIp = HttpContext.Connection.RemoteIpAddress?.ToString();
            
            using var httpClient = new HttpClient();
            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("secret", secret ?? string.Empty),
                new KeyValuePair<string, string>("response", recaptchaResponse),
                new KeyValuePair<string, string>("remoteip", remoteIp ?? string.Empty)
            });
            var response = await httpClient.PostAsync("https://challenges.cloudflare.com/turnstile/v0/siteverify", content);
            var json = await response.Content.ReadAsStringAsync();
            var turnstileResult = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(json);
            return turnstileResult.GetProperty("success").GetBoolean();
        }

        private string StripHtmlTags(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            // Remove HTML tags using regex
            var result = System.Text.RegularExpressions.Regex.Replace(input, "<[^>]*>", string.Empty);
            
            // Decode HTML entities
            result = System.Net.WebUtility.HtmlDecode(result);
            
            // Clean up excessive whitespace
            result = System.Text.RegularExpressions.Regex.Replace(result, @"\s+", " ").Trim();
            
            return result;
        }

        /// <summary>
        /// Converts all HTTP URLs in HTML content to HTTPS for security
        /// </summary>
        private string ConvertHttpToHttps(string content)
        {
            if (string.IsNullOrEmpty(content))
                return content;

            try
            {
                // Replace HTTP URLs in src attributes
                content = System.Text.RegularExpressions.Regex.Replace(
                    content,
                    @"src=[""']http://([^""']+)[""']",
                    @"src=""https://$1""",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase
                );

                // Replace HTTP URLs in href attributes
                content = System.Text.RegularExpressions.Regex.Replace(
                    content,
                    @"href=[""']http://([^""']+)[""']",
                    @"href=""https://$1""",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase
                );

                // Replace HTTP URLs in style attributes (background images, etc.)
                content = System.Text.RegularExpressions.Regex.Replace(
                    content,
                    @"url\(['""']?http://([^)'""\s]+)['""']?\)",
                    @"url('https://$1')",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase
                );

                return content;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error converting HTTP to HTTPS in content");
                return content;
            }
        }
    }
}
