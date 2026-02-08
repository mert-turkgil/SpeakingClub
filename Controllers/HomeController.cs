using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
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
        private readonly IDictionaryService _dictionaryService;
        private readonly IDeeplService _deeplService;
        private readonly UserManager<User> _userManager;

        public HomeController(
            IConfiguration configuration,
            ILogger<HomeController> logger,
            LanguageService localization,
            IUnitOfWork unitOfWork,
            IMemoryCache memoryCache,
            IEmailSender emailSender,
            IDeeplService deeplService,
            IDictionaryService dictionaryService,
            UserManager<User> userManager)
        {
            _configuration = configuration;
            _memoryCache = memoryCache;
            _logger = logger;
            _localization = localization;
            _unitOfWork = unitOfWork;
            _emailSender = emailSender;
            _deeplService = deeplService;
            _dictionaryService = dictionaryService;
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
                ViewData["Canonical"] = "https://almanca-konus.com" + canonicalPath;
            if (ogImage != null)
                ViewData["OgImage"] = ogImage;
            
            // Set hreflang alternates
            ViewData["HreflangTr"] = ViewData["HreflangTr"];
            ViewData["HreflangDe"] = ViewData["HreflangDe"];
        }

        #region SetLanguage
        public IActionResult SetLanguage(string culture, string returnUrl)
        {
            Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
                new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1) }
            );
            return Redirect(Request.Headers["Referer"].ToString());
        }
        #endregion

        #region Anasayfa
        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            if (IsGerman())
            {
                SetSeoData(
                    "Almanca Konuşma Kulübü | Deutsch Sprechen Lernen",
                    "Lernen Sie Deutsch sprechen mit interaktiven Quizzen, Wörterbuch und spannenden Inhalten. Kostenlos üben!",
                    "Deutsch lernen, Deutsch sprechen, Sprachclub, Quiz, Wörterbuch, Online Deutsch",
                    "/");
            }
            else
            {
                SetSeoData(
                    "Almanca Konuşma Kulübü | Online Almanca Öğren",
                    "Almanca Konuşma Kulübü - İnteraktif sınavlar, sözlük ve ilgi çekici içeriklerle Almanca öğrenin. Ücretsiz pratik yapın!",
                    "almanca öğren, almanca konuşma, almanca kulüp, almanca quiz, almanca sözlük, online almanca",
                    "/");
            }
            ViewData["HreflangTr"] = "https://almanca-konus.com/";
            ViewData["HreflangDe"] = "https://almanca-konus.com/";

            var entityBlogs = await _unitOfWork.Blogs.GetAllAsync();
            var selectedBlogs = entityBlogs.Where(b => b.isHome == true).ToList();
            var currentCulture = System.Globalization.CultureInfo.CurrentCulture.Name;
            var langCode = currentCulture.Substring(0, 2).ToLower();
            var model = new IndexModel
            {
                BlogItems = selectedBlogs.Any() ?
                    selectedBlogs.Select(b => new SpeakingClub.Entity.Blog
                    {
                        BlogId = b.BlogId,
                        Image = b.Image,
                        Url = b.Url,
                        RawYT = b.RawYT,
                        RawMaps = b.RawMaps,
                        Title = _localization.GetKey($"Title_{b.BlogId}_{b.Url}_{langCode}")?.Value ?? b.Title,
                        Content = _localization.GetKey($"Content_{b.BlogId}_{b.Url}_{langCode}")?.Value ?? b.Content,
                        Date = b.Date,
                        Author = b.Author,
                        Tags = b.Tags
                    }).ToList() :
                    entityBlogs.Select(b => new SpeakingClub.Entity.Blog
                    {
                        BlogId = b.BlogId,
                        Image = b.Image,
                        Url = b.Url,
                        RawYT = b.RawYT,
                        RawMaps = b.RawMaps,
                        Title = _localization.GetKey($"Title_{b.BlogId}_{b.Url}_{langCode}")?.Value ?? b.Title,
                        Content = _localization.GetKey($"Content_{b.BlogId}_{b.Url}_{langCode}")?.Value ?? b.Content,
                        Date = b.Date,
                        Author = b.Author,
                        Tags = b.Tags
                    }).ToList()
            };

            return View(model);
        }
        #endregion

        #region Gizlilik
        [HttpGet("privacy")]
        [HttpGet("gizlilik")]
        [HttpGet("datenschutz")]
        public IActionResult Privacy()
        {
            if (IsGerman())
            {
                SetSeoData(
                    "Datenschutzerklärung | Almanca Konuşma Kulübü",
                    "Datenschutzerklärung der Almanca Konuşma Kulübü Plattform.",
                    "Datenschutz, Privatsphäre, DSGVO",
                    "/datenschutz");
            }
            else
            {
                SetSeoData(
                    "Gizlilik Politikası | Almanca Konuşma Kulübü",
                    "Almanca Konuşma Kulübü gizlilik politikası ve kişisel verilerin korunması.",
                    "gizlilik, kişisel veri, KVKK",
                    "/gizlilik");
            }
            ViewData["HreflangTr"] = "https://almanca-konus.com/gizlilik";
            ViewData["HreflangDe"] = "https://almanca-konus.com/datenschutz";
            return View();
        }
        #endregion

        #region Hakkında
        [HttpGet("about")]
        [HttpGet("hakkimizda")]
        [HttpGet("ueber-uns")]
        public IActionResult About()
        {
            ViewBag.TurnstileSiteKey = _configuration["Turnstile:SiteKey"];
            
            if (IsGerman())
            {
                SetSeoData(
                    "Über Uns | Almanca Konuşma Kulübü",
                    "Erfahren Sie mehr über unser Team und unsere Mission, Deutsch-Lernenden zu helfen. Kontaktieren Sie uns!",
                    "über uns, Kontakt, Deutsch lernen, Sprachschule, Team",
                    "/ueber-uns");
            }
            else
            {
                SetSeoData(
                    "Hakkımızda | Almanca Konuşma Kulübü",
                    "Ekibimiz ve Almanca öğrenenlere yardımcı olma misyonumuz hakkında bilgi alın. Bizimle iletişime geçin!",
                    "hakkımızda, iletişim, almanca öğren, dil okulu, ekip",
                    "/hakkimizda");
            }
            ViewData["HreflangTr"] = "https://almanca-konus.com/hakkimizda";
            ViewData["HreflangDe"] = "https://almanca-konus.com/ueber-uns";
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
            string? recaptchaResponse = Request.Form["g-recaptcha-response"];
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
                return RedirectToAction("About");
            }

            TempData["ContactSuccess"] = "Your message was sent successfully!";
            return RedirectToAction("About");
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
        public async Task<IActionResult> Blog(string category, string tag, string searchTerm, int page = 1)
        {
            // SEO metadata for blog listing
            if (IsGerman())
            {
                SetSeoData(
                    "Blog & Artikel | Almanca Konuşma Kulübü",
                    "Entdecken Sie unsere Artikel und Beiträge rund ums Deutschlernen. Tipps, Grammatik, Vokabeln und mehr.",
                    "Blog, Deutsch lernen, Artikel, Grammatik, Vokabeln, Tipps, Deutschkurs",
                    "/beitraege");
            }
            else
            {
                SetSeoData(
                    "Blog & Yazılar | Almanca Konuşma Kulübü",
                    "Almanca öğrenme ile ilgili yazılarımızı keşfedin. İpuçları, gramer, kelime bilgisi ve daha fazlası.",
                    "blog, almanca öğren, yazılar, gramer, kelime, ipuçları, almanca kursu",
                    "/yazilar");
            }
            ViewData["HreflangTr"] = "https://almanca-konus.com/yazilar";
            ViewData["HreflangDe"] = "https://almanca-konus.com/beitraege";

            // Retrieve all blogs from the database and convert to IQueryable for filtering.
            var entityBlogs = await _unitOfWork.Blogs.GetAllAsync();
            var blogsQuery = entityBlogs.AsQueryable();

            // Filter by category if provided.
            if (!string.IsNullOrEmpty(category) && category != "All Categories")
            {
                blogsQuery = blogsQuery.Where(b => b.Category != null && b.Category.Name == category);
            }

            // Filter by tag if provided.
            if (!string.IsNullOrEmpty(tag))
            {
                blogsQuery = blogsQuery.Where(b => b.Tags.Any(t => t.Name.ToLower().Contains(tag.ToLower())));
            }

            // Filter by search term on title or content.
            if (!string.IsNullOrEmpty(searchTerm))
            {
                blogsQuery = blogsQuery.Where(b => b.Title.Contains(searchTerm) || b.Content.Contains(searchTerm));
            }

            // Paging logic.
            int pageSize = 9;
            int totalBlogs = blogsQuery.Count();
            int totalPages = (int)Math.Ceiling(totalBlogs / (double)pageSize);

            var blogs = blogsQuery
                .OrderByDescending(b => b.Date)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();
            // Get current culture code (e.g.,   "tr", etc.)
            var currentCulture = System.Globalization.CultureInfo.CurrentCulture.Name;
            var langCode = currentCulture.Substring(0, 2).ToLower();

            // Update each blog with its translated Title, Content, and Slug if available.
            foreach (var blog in blogs)
            {
                var translation = _unitOfWork.BlogTranslations
                    .GetByBlogAndLanguageAsync(blog.BlogId, langCode).Result;
                blog.Title = translation?.Title ?? blog.Title;
                blog.Content = translation?.Content ?? blog.Content;
                // Use translated slug for localized URLs
                var translatedSlug = translation?.Slug;
                if (!string.IsNullOrEmpty(translatedSlug))
                {
                    blog.Url = translatedSlug;
                    blog.Slug = translatedSlug;
                }
                else if (!string.IsNullOrEmpty(blog.Slug))
                {
                    blog.Url = blog.Slug;
                }
            }
            // Get the list of categories.
            var categoriesFromRepo = await _unitOfWork.Categories.GetAllAsync();
            var categoryNames = categoriesFromRepo.Select(c => c.Name).ToList();
            categoryNames.Insert(0, "All Categories");

            // Get the list of available tags from all blogs (distinct)
            var availableTags = entityBlogs
                .SelectMany(b => b.Tags.Select(t => t.Name))
                .Distinct()
                .OrderBy(t => t)
                .ToList();

            // Build the view model.
            var model = new BlogFilterViewModel
            {
                Category = category,
                Tag = tag,
                SearchTerm = searchTerm,
                CurrentPage = page,
                TotalPages = totalPages,
                TotalBlogs = totalBlogs,
                Blogs = blogs, // Blogs are of type SpeakingClub.Entity.Blog
                Categories = categoryNames,
                AvailableTags = availableTags,

                // UI text labels (static or from configuration)
                BlogList_Title = _localization.GetKey("Blog").Value,
                BlogList_Description = _localization.GetKey("BlogDesc").Value,
                BlogList_SearchLabel = _localization.GetKey("Search").Value,
                BlogList_AllCategories = _localization.GetKey("CategoryLabel").Value,
                BlogList_TagLabel = _localization.GetKey("Tag").Value,              // New label for tag filter
                BlogList_TagPlaceholder = "Select a tag", // Optional placeholder text
                BlogList_ApplyFiltersButton = _localization.GetKey("Search").Value,
                BlogList_NoPostsMessage = "No posts found.",
                BlogList_ReadMore = _localization.GetKey("ReadMore").Value,
            };

            return View(model);
        }

        [HttpGet("blog/{url}")]
        [HttpGet("yazilar/{url}")]
        [HttpGet("beitraege/{url}")]
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

                var currentCulture = CultureInfo.CurrentCulture.Name;
                var langCode = currentCulture.Substring(0, 2).ToLower();
                var translation = await _unitOfWork.BlogTranslations
                    .GetByBlogAndLanguageAsync(blog.BlogId, langCode);
                blog.Title = translation?.Title ?? blog.Title;
                blog.Content = translation?.Content ?? blog.Content;

                // SEO metadata from translation or blog defaults
                var metaTitle = translation?.MetaTitle ?? translation?.Title ?? blog.Title;
                var metaDesc = translation?.MetaDescription ?? translation?.Description ?? blog.Description ?? "";
                var metaKeywords = translation?.MetaKeywords ?? "";
                var ogTitle = translation?.OgTitle ?? metaTitle;
                var ogDesc = translation?.OgDescription ?? metaDesc;
                var blogSlug = translation?.Slug ?? blog.Slug ?? url;

                var canonicalPath = IsGerman() ? $"/beitraege/{blogSlug}" : $"/yazilar/{blogSlug}";
                SetSeoData(metaTitle, metaDesc, metaKeywords, canonicalPath);
                ViewData["OgType"] = "article";
                ViewData["OgImage"] = !string.IsNullOrEmpty(blog.Image) ? $"https://almanca-konus.com{blog.Image}" : null;

                // Hreflang for blog detail
                var trTranslation = await _unitOfWork.BlogTranslations.GetByBlogAndLanguageAsync(blog.BlogId, "tr");
                var deTranslation = await _unitOfWork.BlogTranslations.GetByBlogAndLanguageAsync(blog.BlogId, "de");
                var trSlug = trTranslation?.Slug ?? blog.Slug ?? url;
                var deSlug = deTranslation?.Slug ?? blog.Slug ?? url;
                ViewData["HreflangTr"] = $"https://almanca-konus.com/yazilar/{trSlug}";
                ViewData["HreflangDe"] = $"https://almanca-konus.com/beitraege/{deSlug}";

                // Article Schema.org structured data
                ViewData["ArticleSchema"] = $@"<script type=""application/ld+json"">
                {{
                    ""@context"": ""https://schema.org"",
                    ""@type"": ""Article"",
                    ""headline"": ""{System.Text.Encodings.Web.JavaScriptEncoder.Default.Encode(metaTitle)}"",
                    ""description"": ""{System.Text.Encodings.Web.JavaScriptEncoder.Default.Encode(metaDesc)}"",
                    ""author"": {{
                        ""@type"": ""Person"",
                        ""name"": ""{blog.Author ?? "Suna Türkgil"}""
                    }},
                    ""datePublished"": ""{blog.Date:yyyy-MM-ddTHH:mm:ssZ}"",
                    ""dateModified"": ""{(blog.LastModified ?? blog.Date):yyyy-MM-ddTHH:mm:ssZ}"",
                    ""image"": ""{(string.IsNullOrEmpty(blog.Image) ? "https://almanca-konus.com/img/header_logo.png" : $"https://almanca-konus.com{blog.Image}")}"",
                    ""publisher"": {{
                        ""@type"": ""Organization"",
                        ""name"": ""Almanca Konuşma Kulübü"",
                        ""logo"": {{
                            ""@type"": ""ImageObject"",
                            ""url"": ""https://almanca-konus.com/img/header_logo.png""
                        }}
                    }},
                    ""mainEntityOfPage"": ""https://almanca-konus.com{canonicalPath}""
                }}
                </script>";

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

                // Create the view model.
                var viewModel = new BlogDetailViewModel
                {
                    BlogId = blog.BlogId,
                    Title = blog.Title,
                    Content = blog.Content,
                    Date = blog.Date,
                    RawMaps = blog.RawMaps,
                    RawYT = blog.RawYT,
                    Author = blog.Author??"Unknown",
                    Image = blog.Image,
                    Tags = blog.Tags?.ToList() ?? new List<Tag>(),
                    Quizzes = blog.Quiz,
                    ViewCount = blog.ViewCount
                };

                // Adjusting quiz question retrieval:
                if (blog.SelectedQuestionId.HasValue)
                {
                    // Try to locate the question by its ID among all questions of the quizzes.
                    var selectedQuestion = blog.Quiz.SelectMany(q => q.Questions)
                                                    .FirstOrDefault(q => q.Id == blog.SelectedQuestionId.Value);
                    viewModel.QuizQuestion = selectedQuestion;
                }
                else if (blog.Quiz != null && blog.Quiz.Any())
                {
                    // Fallback: select the first question from the first quiz.
                    var firstQuiz = blog.Quiz.First();
                    if (firstQuiz.Questions != null && firstQuiz.Questions.Any())
                    {
                        viewModel.QuizQuestion = firstQuiz.Questions.First();
                    }
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

        #endregion

        #region Sözlük
        [HttpGet("words")]
        [HttpGet("sozluk")]
        [HttpGet("woerterbuch")]
        public async Task<IActionResult> Words(string searchTerm)
        {
            ViewBag.TurnstileSiteKey = _configuration["Turnstile:SiteKey"];
            var currentCulture = CultureInfo.CurrentCulture.Name;
            var langCode = currentCulture.Substring(0, 2).ToLower();
            
            // SEO metadata for dictionary
            if (IsGerman())
            {
                SetSeoData(
                    "Wörterbuch | Almanca Konuşma Kulübü",
                    "Deutsches Wörterbuch mit Definitionen, Aussprache, Synonymen und Beispielen. Suchen Sie jedes deutsche Wort!",
                    "Wörterbuch, Deutsch, Definition, Aussprache, Synonyme, Übersetzung",
                    "/woerterbuch");
            }
            else
            {
                SetSeoData(
                    "Sözlük | Almanca Konuşma Kulübü",
                    "Almanca sözlük - tanım, telaffuz, eş anlamlılar ve örneklerle her Almanca kelimeyi arayın!",
                    "sözlük, almanca, tanım, telaffuz, eş anlamlı, çeviri",
                    "/sozluk");
            }
            ViewData["HreflangTr"] = "https://almanca-konus.com/sozluk";
            ViewData["HreflangDe"] = "https://almanca-konus.com/woerterbuch";

            var model = new WordViewModel
            {
                SearchTerm = searchTerm ?? string.Empty
            };

            // Words GET action'ın başına ekle:
            string userIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            string key = $"WordsSearch:{userIp}";
            int count = _memoryCache.GetOrCreate(key, entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1);
                return 0;
            });
            if (count >= 15) // 1 dakikada 15 aramadan fazlası engellenir
            {
                return Content("Çok fazla arama yaptınız, lütfen bir süre sonra tekrar deneyin.");
            }
            _memoryCache.Set(key, count + 1, TimeSpan.FromMinutes(1));


            if (!string.IsNullOrEmpty(searchTerm))
            {
                // First, check your local database (case-insensitive search).
                var wordFromDb = await _unitOfWork.Words.GetWordByTermAsync(searchTerm);

                if (wordFromDb != null && !string.IsNullOrWhiteSpace(wordFromDb.Definition))
                {
                    var eksikolabilir = await _dictionaryService.GetWordDetailsAsync(wordFromDb.Term.ToLower(), langCode, "de");
                    model.Word = new Word
                    {
                        Definition = wordFromDb.Definition ?? eksikolabilir?.Word?.Definition ?? "Error:404",
                        Origin = wordFromDb.Origin ?? eksikolabilir?.Word?.Origin ?? "Error:404",
                        Pronunciation = wordFromDb.Pronunciation ?? eksikolabilir?.Word?.Pronunciation ?? "Error404",
                        Synonyms = wordFromDb.Synonyms ?? eksikolabilir?.Word?.Synonyms ?? "Error404",
                        Example = wordFromDb.Example ?? eksikolabilir?.Word?.Example ?? "Error404",
                        IsFromApi = false,
                        Term = wordFromDb.Term ?? eksikolabilir?.Word?.Term ?? "Error404"
                    };
                }
                else
                {
                    // Try fetching details from the Free Dictionary API.
                    var dictionaryResult = await _dictionaryService.GetWordDetailsAsync(searchTerm, langCode, "de");

                    if (dictionaryResult?.Word != null)
                    {
                        model.Word = new Word
                        {
                            Term = dictionaryResult.Word.Term,
                            Definition = dictionaryResult.Word.Definition.ToString(),
                            IsFromApi = true,
                        };
                    }
                    else
                    {

                        // Use DeepL API as a fallback for translation.
                        var deeplTranslation = await _deeplService.GetDefinitionByCultureAsync(searchTerm, langCode);

                        if (!string.IsNullOrEmpty(deeplTranslation))
                        {
                            model.Word = new Word
                            {
                                Term = searchTerm,
                                Definition = deeplTranslation.ToString(),
                                IsFromApi = true
                            };
                        }
                        else
                        {
                            model.WarningMessage = "We are working on it. No definition available at this time.";
                        }
                    }
                }
            }

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
            // SEO metadata for quizzes
            if (IsGerman())
            {
                var levelText = !string.IsNullOrEmpty(level) ? $" - {level}" : "";
                SetSeoData(
                    $"Deutsch-Prüfungen{levelText} | Almanca Konuşma Kulübü",
                    "Testen Sie Ihr Deutsch mit unseren interaktiven Quizzen. Verschiedene Schwierigkeitsstufen für Anfänger bis Fortgeschrittene.",
                    "Deutsch-Quiz, Prüfung, Deutschtest, A1, A2, B1, B2, C1, Grammatik-Quiz, Vokabel-Quiz",
                    "/pruefungen" + (level != null ? $"/{level.ToLower()}" : ""));
            }
            else
            {
                var levelText = !string.IsNullOrEmpty(level) ? $" - {level}" : "";
                SetSeoData(
                    $"Almanca Sınavlar{levelText} | Almanca Konuşma Kulübü",
                    "Almanca bilginizi interaktif sınavlarla test edin. Başlangıçtan ileri seviyeye farklı zorluk seviyeleri.",
                    "almanca quiz, sınav, almanca test, A1, A2, B1, B2, C1, gramer quiz, kelime quiz",
                    "/sinavlar" + (level != null ? $"/{level.ToLower()}" : ""));
            }
            ViewData["HreflangTr"] = "https://almanca-konus.com/sinavlar" + (level != null ? $"/{level.ToLower()}" : "");
            ViewData["HreflangDe"] = "https://almanca-konus.com/pruefungen" + (level != null ? $"/{level.ToLower()}" : "");
            
            // Quiz Schema.org structured data
            ViewData["ArticleSchema"] = $@"<script type=""application/ld+json"">
            {{
                ""@@context"": ""https://schema.org"",
                ""@@type"": ""ItemList"",
                ""name"": ""{(IsGerman() ? "Deutsch-Prüfungen" : "Almanca Sınavlar")}"",
                ""description"": ""{(IsGerman() ? "Interaktive Deutsch-Quizze" : "Almanca interaktif sınavlar")}"",
                ""url"": ""https://almanca-konus.com{(IsGerman() ? "/pruefungen" : "/sinavlar")}""
            }}
            </script>";

            // Retrieve all quizzes
            var allQuizzes = await _unitOfWork.Quizzes.GetAllAsync();

            // Filter by level if provided
            if (!string.IsNullOrEmpty(level))
            {
                var filteredQuizzes = allQuizzes.Where(q => q.Tags != null && q.Tags.Any(t => t.Name.Equals(level, StringComparison.OrdinalIgnoreCase))).ToList();
                if (filteredQuizzes.Any())
                {
                    allQuizzes = filteredQuizzes;
                }
                ViewBag.SelectedLevel = level;
            }

            // Get the current user if logged in
            var user = await _userManager.GetUserAsync(User);
            var userSubmissions = new List<QuizSubmission>();

            if (user != null)
            {
                var submissions = await _unitOfWork.QuizSubmissions.GetAllAsync();
                userSubmissions = submissions.Where(s => s.UserId == user.Id).ToList();
            }

            // Pre-fetch teacher display names
            var teacherUserNames = allQuizzes.Select(q => q.TeacherName).Where(t => !string.IsNullOrEmpty(t)).Distinct().ToList();
            var teacherDisplayNames = new Dictionary<string, string>();
            foreach (var teacherUserName in teacherUserNames)
            {
                if (!string.IsNullOrEmpty(teacherUserName))
                {
                    var teacher = await _userManager.FindByNameAsync(teacherUserName);
                    teacherDisplayNames[teacherUserName] = teacher != null ? $"{teacher.FirstName} {teacher.LastName}" : teacherUserName;
                }
            }

            Func<SpeakingClub.Entity.Quiz, QuizSummaryViewModel> mapQuiz = q =>
            {
                var lastSubmission = userSubmissions
                    .Where(s => s.QuizId == q.Id)
                    .OrderByDescending(s => s.SubmissionDate)
                    .FirstOrDefault();

                IEnumerable<AttemptDetailViewModel>? details = null;
                if (lastSubmission != null && lastSubmission.QuizResponses != null)
                {
                    details = lastSubmission.QuizResponses.Select(r =>
                    {
                        var question = r.QuizAnswer?.Question;
                        var correctAnswer = question?.Answers.FirstOrDefault(a => a.IsCorrect)?.AnswerText ?? "N/A";
                        var yourAnswer = r.QuizAnswer?.AnswerText ?? r.AnswerText ?? "No answer";
                        bool isCorrect = r.QuizAnswer != null && r.QuizAnswer.IsCorrect;
                        return new AttemptDetailViewModel
                        {
                            QuestionId = question?.Id ?? 0,
                            QuestionText = question?.QuestionText ?? "N/A",
                            YourAnswer = yourAnswer,
                            CorrectAnswer = correctAnswer,
                            TimeTakenSeconds = 0,
                            IsCorrect = isCorrect
                        };
                    });
                }

                return new QuizSummaryViewModel
                {
                    Tags = q.Tags?.ToList() ?? new List<Tag>(),
                    AudioUrl = q.AudioUrl,
                    YouTubeVideoUrl = q.YouTubeVideoUrl,
                    QuizId = q.Id,
                    QuizTitle = q.Title,
                    QuizDescription = q.Description,
                    ImageUrl = !string.IsNullOrEmpty(q.ImageUrl) ? q.ImageUrl : Url.Content("~/img/header_logo.png"),
                    TeacherName = !string.IsNullOrEmpty(q.TeacherName) && teacherDisplayNames.ContainsKey(q.TeacherName) 
                        ? teacherDisplayNames[q.TeacherName] 
                        : "Unknown Instructor",
                    CategoryName = q.Category != null ? q.Category.Name : "General",
                    AttemptCount = userSubmissions.Count(s => s.QuizId == q.Id),
                    LastScore = lastSubmission?.Score,
                    LastAttemptDate = lastSubmission?.SubmissionDate,
                    RecentAttemptDetails = details
                };
            };

            var viewModel = new CombinedQuizzesViewModel
            {
                AvailableQuizzes = allQuizzes.Select(mapQuiz).ToList()
            };

            return View("~/Views/Account/Quizzes.cshtml", viewModel);
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
    }
}
