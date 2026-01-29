using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using SpeakingClub.Data.Abstract;
using SpeakingClub.Entity;
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

        public HomeController(
            IConfiguration configuration,
            ILogger<HomeController> logger,
            LanguageService localization,
            IUnitOfWork unitOfWork,
            IMemoryCache memoryCache,
            IEmailSender emailSender,
            IDeeplService deeplService,
            IDictionaryService dictionaryService)
        {
            _configuration = configuration;
            _memoryCache = memoryCache;
            _logger = logger;
            _localization = localization;
            _unitOfWork = unitOfWork;
            _emailSender = emailSender;
            _deeplService = deeplService;
            _dictionaryService = dictionaryService;
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
            ViewData["Title"] = "Ana Sayfa";
            ViewData["Description"] = "Almanca Konuşma Kulübü - Almanca pratik ve topluluk.";
            ViewData["Keywords"] = "almanca, konuşma, kulüp, speaking, deutsch";

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
        public IActionResult Privacy()
        {
            return View();
        }
        #endregion

        #region Hakkında
        [HttpGet("about")]
        public IActionResult About()
        {
            ViewBag.RecaptchaSiteKey = _configuration["Recaptcha:SiteKey"];
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
        public async Task<IActionResult> Blog(string category, string tag, string searchTerm, int page = 1)
        {
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

            // Update each blog with its translated Title and Content if available.
            foreach (var blog in blogs)
            {
                blog.Title = _unitOfWork.BlogTranslations
                    .GetByBlogAndLanguageAsync(blog.BlogId, langCode)
                    .Result?.Title ?? blog.Title;
                blog.Content = _unitOfWork.BlogTranslations
                    .GetByBlogAndLanguageAsync(blog.BlogId, langCode)
                    .Result?.Content ?? blog.Content;
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
        public async Task<IActionResult> BlogDetail(string url)
        {
            try
            {
                _logger.LogInformation("Fetching blog detail for url {url}.", url);

                // Retrieve the blog using your slug
                var blog = await _unitOfWork.Blogs.GetByUrlAsync(url);
                if (blog == null)
                {
                    _logger.LogWarning("Blog with slug {url} not found.", url);
                    return NotFound();
                }

                var currentCulture = CultureInfo.CurrentCulture.Name;
                var langCode = currentCulture.Substring(0, 2).ToLower();
                blog.Title = _unitOfWork.BlogTranslations
                    .GetByBlogAndLanguageAsync(blog.BlogId, langCode)
                    .Result?.Title ?? blog.Title;
                blog.Content = _unitOfWork.BlogTranslations
                    .GetByBlogAndLanguageAsync(blog.BlogId, langCode)
                    .Result?.Content ?? blog.Content;

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
                    Author = blog.Author,
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
        public async Task<IActionResult> Words(string searchTerm)
        {
            ViewBag.RecaptchaSiteKey = _configuration["Recaptcha:SiteKey"];
            var currentCulture = CultureInfo.CurrentCulture.Name;
            var langCode = currentCulture.Substring(0, 2).ToLower();

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

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
        
        private async Task<bool> RecaptchaIsValid(string recaptchaResponse)
        {
            var secret = _configuration["Recaptcha:SecretKey"];
            using var httpClient = new HttpClient();
            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("secret", secret ?? string.Empty),
                new KeyValuePair<string, string>("response", recaptchaResponse)
            });
            var response = await httpClient.PostAsync("https://www.google.com/recaptcha/api/siteverify", content);
            var json = await response.Content.ReadAsStringAsync();
            // Çok temel bir doğrulama, istersen JSON parse ile daha sağlam hale getirebiliriz.
            return json.Contains("\"success\": true");
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
