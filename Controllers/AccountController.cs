using System;
using System.Dynamic;
using System.Globalization;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
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
    [Route("[controller]/[action]")]
    public class AccountController : Controller
    {
        private static Dictionary<string, DateTime> _contactRateLimit = new();
        private static Dictionary<string, (int Count, DateTime FirstAttempt)> _contactAttempts = new();
        private static readonly object _rateLimitLock = new();
        private readonly IMemoryCache _memoryCache;
        private readonly IConfiguration _configuration;
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly IEmailSender _emailSender;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            IUnitOfWork unitOfWork,
            IConfiguration configuration,
            ILogger<AccountController> logger,
            IMemoryCache memoryCache,
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            IEmailSender emailSender)
        {
            _configuration = configuration;
            _memoryCache = memoryCache;
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _signInManager = signInManager;
            _emailSender = emailSender;
            _logger = logger;
        }
        #region Quiz
        // Helper method to get teacher display name
        private async Task<string> GetTeacherDisplayName(string? userName)
        {
            if (string.IsNullOrEmpty(userName))
                return "Unknown Instructor";
            
            var teacher = await _userManager.FindByNameAsync(userName);
            if (teacher == null)
                return userName; // Fallback to stored name
            
            var fullName = string.Join(" ", new[] { teacher.FirstName, teacher.LastName }
                .Where(s => !string.IsNullOrWhiteSpace(s)));
            
            return !string.IsNullOrWhiteSpace(fullName) ? fullName : (userName ?? "Unknown Instructor");
        }

        [AllowAnonymous]
        public async Task<IActionResult> Quizzes(string? level)
        {
            if (Request.Query.TryGetValue("warning", out var warningValue))
            {
                string warning = warningValue.ToString(); // Convert to string
                switch (warning)
                {
                    case "FullscreenExit":
                        TempData["Warning"] = "Exam canceled because you exited fullscreen mode.";
                        break;
                    case "LeaveScreen":
                        TempData["Warning"] = "Exam canceled because you left the screen.";
                        break;
                    case "Inactivity":
                        TempData["Warning"] = "Exam canceled due to inactivity.";
                        break;
                }
            }
            // Retrieve all quizzes from your repository
            var allQuizzes = await _unitOfWork.Quizzes.GetAllAsync();

            // Filter by level if provided
            if (!string.IsNullOrEmpty(level))
            {
                var filteredQuizzes = allQuizzes.Where(q => q.Tags != null && q.Tags.Any(t => t.Name.Equals(level, StringComparison.OrdinalIgnoreCase))).ToList();
                // Only apply filter if it returns results, otherwise show all
                if (filteredQuizzes.Any())
                {
                    allQuizzes = filteredQuizzes;
                }
                // Store the requested level for the view
                ViewBag.SelectedLevel = level;
            }

            // Get the current user if logged in
            var user = await _userManager.GetUserAsync(User);
            var userSubmissions = new List<QuizSubmission>();

            // Only fetch user submissions if logged in
            if (user != null)
            {
                var submissions = await _unitOfWork.QuizSubmissions.GetAllAsync();
                userSubmissions = submissions.Where(s => s.UserId == user.Id).ToList();
            }

            // Pre-fetch all teacher display names
            var teacherUserNames = allQuizzes.Select(q => q.TeacherName).Where(t => !string.IsNullOrEmpty(t)).Distinct().ToList();
            var teacherDisplayNames = new Dictionary<string, string>();
            foreach (var teacherUserName in teacherUserNames)
            {
                if (!string.IsNullOrEmpty(teacherUserName))
                {
                    teacherDisplayNames[teacherUserName] = await GetTeacherDisplayName(teacherUserName);
                }
            }

            // Map each quiz to a QuizSummaryViewModel.
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
                        int timeTaken = 0; // Replace with actual timing info if available.
                        bool isCorrect = r.QuizAnswer != null && r.QuizAnswer.IsCorrect;
                        return new AttemptDetailViewModel
                        {
                            QuestionId = question?.Id ?? 0,
                            QuestionText = question?.QuestionText ?? "N/A",
                            YourAnswer = yourAnswer,
                            CorrectAnswer = correctAnswer,
                            TimeTakenSeconds = timeTaken,
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

            return View(viewModel);
        }

        [Authorize]
        public async Task<IActionResult> ReviewQuiz(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return NotFound("User not found.");

            // Use a repository method that includes navigation properties
            var submission = await _unitOfWork.QuizSubmissions
                .GetLatestSubmissionByUserAndQuiz(user.Id, id); // Implement this method

            if (submission == null)
                return NotFound("No submission found.");

            var details = submission.QuizResponses.Select(r =>
            {
                var question = r.QuizAnswer?.Question;
                return new AttemptDetailViewModel
                {
                    QuestionId = question?.Id ?? 0,
                    QuestionText = question?.QuestionText ?? "N/A",
                    YourAnswer = r.QuizAnswer?.AnswerText ?? r.AnswerText ?? "No answer",
                    CorrectAnswer = question?.Answers.FirstOrDefault(a => a.IsCorrect)?.AnswerText ?? "N/A",
                    TimeTakenSeconds = r.TimeTakenSeconds,
                    IsCorrect = r.QuizAnswer?.IsCorrect ?? false
                };
            }).ToList();

            var viewModel = new QuizReviewViewModel
            {
                QuizId = submission.QuizId,
                QuizTitle = submission.Quiz?.Title,
                SubmissionDate = submission.SubmissionDate,
                Score = submission.Score,
                Details = details,
                TotalTimeTaken = details.Sum(d => d.TimeTakenSeconds)
            };

            return View(viewModel);
        }


        [Authorize]
        public async Task<IActionResult> StartQuiz(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return NotFound("User not found.");

            // Optionally attach if needed:
            _unitOfWork.Users.Attach(user);

            var quiz = await _unitOfWork.Quizzes.GetByIdAsync(id);
            if (quiz == null)
                return NotFound();

            return View(quiz);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> SubmitQuiz(string? QuizId, Dictionary<int, int>? responses, int ElapsedTime)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                // This should stop everything!
                return NotFound("User not found.");
            }
            if (string.IsNullOrEmpty(user.Id))
            {
                // Log this as a critical error
                throw new Exception("Authenticated user has no Id!");
            }

            // If no responses were provided, redirect back to the quiz page
            if (responses == null || responses.Count == 0)
            {
                TempData["Warning"] = "You must answer at least one question!";
                return RedirectToAction("Quizzes");
            }
            else
            {
                // Ensure QuizId is valid
                if (string.IsNullOrEmpty(QuizId) || !int.TryParse(QuizId, out int quizId))
                {
                    return BadRequest("Invalid or missing Quiz ID.");
                }

                var quiz = await _unitOfWork.Quizzes.GetByIdAsync(quizId);
                if (quiz == null)
                    return NotFound("Quiz not found.");



                var submission = new QuizSubmission
                {
                    UserId = user.Id,
                    QuizId = quizId,
                    SubmissionDate = DateTime.UtcNow,
                    Score = 0,
                    AttemptNumber = 1
                };

                int score = 0;

                foreach (var question in quiz.Questions)
                {
                    var selectedAnswerId = responses.ContainsKey(question.Id) ? responses[question.Id] : (int?)null;
                    var correctAnswer = question.Answers.FirstOrDefault(a => a.IsCorrect);

                    if (selectedAnswerId == correctAnswer?.Id)
                        score++;

                    submission.QuizResponses.Add(new QuizResponse
                    {
                        QuizAnswerId = selectedAnswerId,
                        AnswerText = selectedAnswerId == null ? "No answer" : null,
                        TimeTakenSeconds = ElapsedTime / Math.Max(1, quiz.Questions.Count)
                    });
                }

                submission.Score = (int)((double)score / quiz.Questions.Count * 100);

                await _unitOfWork.QuizSubmissions.AddAsync(submission);
                await _unitOfWork.SaveAsync();

                TempData["Success"] = $"Quiz attempt recorded. Your score: {submission.Score}%";
                return RedirectToAction(nameof(Quizzes));
            }
        }


        #endregion
        #region Account
        [Authorize]
        public async Task<IActionResult> Account()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                TempData["Error"] = "Unable to load user data.";
                return RedirectToAction("Login", "Account");
            }


            var submissions = await _unitOfWork.QuizSubmissions.GetAllAsync();
            var quizzes = await _unitOfWork.Quizzes.GetAllAsync();
            var userSubmissions = submissions.Where(qs => qs.UserId == user.Id).ToList();

            // Check if any quizzes have been completed
            bool hasQuizData = userSubmissions.Any();
            ViewBag.HasQuizData = hasQuizData;

            if (hasQuizData)
            {
                double averageScore = userSubmissions.Average(q => q.Score);
                int totalAttempts = userSubmissions.Count;
                int distinctQuizzes = userSubmissions.Select(s => s.QuizId).Distinct().Count();

                string level;
                if (averageScore >= 90)
                    level = "C2";
                else if (averageScore >= 80)
                    level = "C1";
                else if (averageScore >= 70)
                    level = "B2";
                else if (averageScore >= 60)
                    level = "B1";
                else if (averageScore >= 50)
                    level = "A2";
                else
                    level = "A1";

                // Prepare list of all attempts with quiz title and score (optional, for graphs)
                var attemptsList = userSubmissions
                    .OrderBy(s => s.SubmissionDate)
                    .Select(s =>
                    {
                        dynamic obj = new ExpandoObject();
                        obj.QuizTitle = quizzes.FirstOrDefault(q => q.Id == s.QuizId)?.Title ?? "Unknown";
                        obj.Score = s.Score;
                        obj.Date = s.SubmissionDate;
                        return obj;
                    })
                    .ToList();

                ViewBag.AttemptsList = attemptsList;
                ViewBag.UserLevel = level;
                ViewBag.Progress = averageScore;
                ViewBag.TotalAttempts = totalAttempts;
                ViewBag.DistinctQuizzes = distinctQuizzes;
            }
            else
            {
                ViewBag.UserLevel = null;
                ViewBag.Progress = 0;
                ViewBag.TotalAttempts = 0;
                ViewBag.DistinctQuizzes = 0;
                ViewBag.AttemptsList = new List<object>();
            }

            return View(user);
        }

        #endregion
        #region Manage Account
        [Authorize]
        [HttpGet]
        public IActionResult ChangePassword()
        {
            return View();
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.GetUserAsync(User);
            var result = await _userManager.ChangePasswordAsync(user!, model.CurrentPassword, model.NewPassword);

            if (result.Succeeded)
            {
                await _signInManager.RefreshSignInAsync(user!);
                TempData["Success"] = "Your password has been changed successfully!";
                return RedirectToAction(nameof(Account));
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);

            return View(model);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAccount()
        {
            var user = await _userManager.GetUserAsync(User);

            // Check if the user is in Admin or Root role
            if (await _userManager.IsInRoleAsync(user!, "Admin") || await _userManager.IsInRoleAsync(user!, "Root"))
            {
                TempData["Error"] = "Admin or Root users cannot delete their accounts.";
                return RedirectToAction(nameof(Account));
            }

            var result = await _userManager.DeleteAsync(user!);

            if (result.Succeeded)
            {
                await _signInManager.SignOutAsync();
                return RedirectToAction("Index", "Home");
            }

            TempData["Error"] = "Error deleting account. Contact support.";
            return RedirectToAction(nameof(Account));
        }


        #endregion



        // GET: /Account/Login
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewBag.TurnstileSiteKey = _configuration["Turnstile:SiteKey"];
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        // POST: /Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            ViewBag.TurnstileSiteKey = _configuration["Turnstile:SiteKey"];
            // Bot detection - check honeypot and other fields
            if (!string.IsNullOrEmpty(Request.Form["honeypot"]) || 
                !string.IsNullOrEmpty(Request.Form["phone"]) ||
                !string.IsNullOrEmpty(Request.Form["website"]))
            {
                // Honeypot filled --> Bot!
                _logger.LogWarning("Bot attempt on Login from {IP}", HttpContext.Connection.RemoteIpAddress?.ToString());
                ModelState.AddModelError("", "Invalid request.");
                return View(model);
            }
            string userIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            lock (_rateLimitLock)
            {
                if (_contactAttempts.TryGetValue(userIp, out var attemptInfo))
                {
                    // If already 5 or more attempts, check for the 1-minute block period.
                    if (attemptInfo.Count >= 5)
                    {
                        if (DateTime.UtcNow - attemptInfo.FirstAttempt < TimeSpan.FromMinutes(1))
                        {
                            ModelState.AddModelError("", "Çok fazla deneme yaptınız. Lütfen 1 dakika sonra tekrar deneyin.");
                            return View(model);
                        }
                        else
                        {
                            // Reset attempt count after blocking period has passed.
                            _contactAttempts[userIp] = (1, DateTime.UtcNow);
                        }
                    }
                    else
                    {
                        _contactAttempts[userIp] = (attemptInfo.Count + 1, attemptInfo.FirstAttempt);
                    }
                }
                else
                {
                    _contactAttempts[userIp] = (1, DateTime.UtcNow);
                }
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
                return View(model);
            }
            _memoryCache.Set(cacheKey, count + 1, TimeSpan.FromMinutes(2));
            // string? recaptchaResponse = Request.Form["g-recaptcha-response"];
            // if (!await RecaptchaIsValid(recaptchaResponse ?? ""))
            // {
            //     ModelState.AddModelError("", "Lütfen robot olmadığınızı doğrulayın (reCAPTCHA).");
            //     return View(model);
            // }

            if (!ModelState.IsValid)
                return View(model);
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                return View(model);
            }

            if (!await _userManager.IsEmailConfirmedAsync(user))
            {
                ModelState.AddModelError(string.Empty, "You must confirm your email before logging in.");
                return View(model);
            }
            
            // Sign in without persistent cookie first to check if credentials are valid
            var result = await _signInManager.PasswordSignInAsync(user, model.Password, false, false);
            
            if (result.Succeeded)
            {
                // Sign out the temporary session
                await _signInManager.SignOutAsync();
                
                // If RememberMe is checked, set cookie expiration based on role
                if (model.RememberMe)
                {
                    var roles = await _userManager.GetRolesAsync(user);
                    TimeSpan cookieExpiration;
                    
                    if (roles.Contains("Teacher") || roles.Contains("Admin") || roles.Contains("Root"))
                    {
                        // Teachers, Admins, and Root users get 30 days
                        cookieExpiration = TimeSpan.FromDays(30);
                    }
                    else if (roles.Contains("Student"))
                    {
                        // Students get 7 days
                        cookieExpiration = TimeSpan.FromDays(7);
                    }
                    else
                    {
                        // Default users get 1 day
                        cookieExpiration = TimeSpan.FromDays(1);
                    }
                    
                    // Create authentication properties with custom expiration
                    var authProperties = new Microsoft.AspNetCore.Authentication.AuthenticationProperties
                    {
                        IsPersistent = true,
                        ExpiresUtc = DateTimeOffset.UtcNow.Add(cookieExpiration)
                    };
                    
                    // Sign in with custom properties
                    await _signInManager.SignInAsync(user, authProperties);
                    _logger.LogInformation($"User logged in with RememberMe for {cookieExpiration.TotalDays} days.");
                }
                else
                {
                    // Regular sign in with default session timeout (60 minutes)
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    _logger.LogInformation("User logged in with session timeout.");
                }
                
                return RedirectToLocal(returnUrl);
            }
            if (result.IsLockedOut)
            {
                _logger.LogWarning("User account locked out.");
                return View("Lockout");
            }

            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            return View(model);
        }

        // GET: /Account/Register
        [HttpGet]
        public IActionResult Register(string? returnUrl = null)
        {
            ViewBag.TurnstileSiteKey = _configuration["Turnstile:SiteKey"];
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        // POST: /Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            ViewBag.TurnstileSiteKey = _configuration["Turnstile:SiteKey"];
            // Bot detection - check honeypot and other fields
            if (!string.IsNullOrEmpty(Request.Form["honeypot"]) || 
                !string.IsNullOrEmpty(Request.Form["phone"]) ||
                !string.IsNullOrEmpty(Request.Form["website"]))
            {
                // Honeypot filled --> Bot!
                _logger.LogWarning("Bot attempt on Register from {IP}", HttpContext.Connection.RemoteIpAddress?.ToString());
                ModelState.AddModelError("", "Invalid request.");
                return View(model);
            }
            string userIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            lock (_rateLimitLock)
            {
                if (_contactAttempts.TryGetValue(userIp, out var attemptInfo))
                {
                    // If already 5 or more attempts, check for the 1-minute block period.
                    if (attemptInfo.Count >= 5)
                    {
                        if (DateTime.UtcNow - attemptInfo.FirstAttempt < TimeSpan.FromMinutes(1))
                        {
                            ModelState.AddModelError("", "Çok fazla deneme yaptınız. Lütfen 1 dakika sonra tekrar deneyin.");
                            return View(model);
                        }
                        else
                        {
                            // Reset attempt count after blocking period has passed.
                            _contactAttempts[userIp] = (1, DateTime.UtcNow);
                        }
                    }
                    else
                    {
                        _contactAttempts[userIp] = (attemptInfo.Count + 1, attemptInfo.FirstAttempt);
                    }
                }
                else
                {
                    _contactAttempts[userIp] = (1, DateTime.UtcNow);
                }
            }

            var ip = HttpContext.Connection.RemoteIpAddress != null
                ? HttpContext.Connection.RemoteIpAddress.ToString()
                : "unknown";
            var cacheKey = $"RegisterAttempts:{ip}";
            int count = _memoryCache.GetOrCreate(cacheKey, entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(2);
                return 0;
            });
            if (count >= 5)
            {
                ModelState.AddModelError("", "Çok fazla deneme yaptınız. Lütfen 2 dakika sonra tekrar deneyin.");
                return View(model);
            }
            _memoryCache.Set(cacheKey, count + 1, TimeSpan.FromMinutes(2));
            string? recaptchaResponse = Request.Form["cf-turnstile-response"];
            if (!await RecaptchaIsValid(recaptchaResponse ?? ""))
            {
                ModelState.AddModelError("", "Lütfen robot olmadığınızı doğrulayın (Turnstile).");
                return View(model);
            }
            if (!ModelState.IsValid)
                return View(model);

            var user = new User { UserName = model.Email, Email = model.Email };
            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                _logger.LogInformation("User created a new account.");
                TempData["ShowSplash"] = true; 
                TempData["SplashMessage"] = "Kayıt başarılı! E-postanızı kontrol edin ve hesabınızı onaylayın."; 
                // Generate confirmation token
                var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                var confirmationLink = Url.Action(
                    nameof(ConfirmEmail), "Account",
                    new { userId = user.Id, token },
                    Request.Scheme);

                // Determine user's current culture
                var currentCulture = CultureInfo.CurrentCulture.Name; // or RequestCulture if applicable

                // Define template path based on culture
                string userTemplatePath;
                if (currentCulture.StartsWith("de", StringComparison.OrdinalIgnoreCase))
                {
                    userTemplatePath = Path.Combine(Directory.GetCurrentDirectory(), "EmailTemplates", "UserConfirm_de.html");
                }
                else
                {
                    userTemplatePath = Path.Combine(Directory.GetCurrentDirectory(), "EmailTemplates", "UserConfirm_tr.html");
                }

                // Read HTML content from template file
                string userEmailBodyTemplate = await System.IO.File.ReadAllTextAsync(userTemplatePath);

                // Replace the placeholder with confirmation link
                string userEmailBody = userEmailBodyTemplate.Replace("{{ConfirmationLink}}", confirmationLink);

                // Email Subject (Localized)
                string emailSubject = currentCulture.StartsWith("de")
                    ? "Bitte bestätigen Sie Ihre E-Mail-Adresse"
                    : currentCulture.StartsWith("tr")
                        ? "Lütfen E-Posta Adresinizi Onaylayın"
                        : "Please Confirm Your Email";

                // Send email directly
                try
                {
                    await _emailSender.SendEmailAsync(user.Email, emailSubject, userEmailBody);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error sending confirmation email.");
                    ModelState.AddModelError(string.Empty, "Unable to send confirmation email. Please contact support.");
                    return View(model);
                }

                // Optionally, sign in user or redirect to a confirmation page
                TempData["Success"] = "Kayıt başarılı! Lütfen e-postanızı kontrol edip hesabınızı onayladıktan sonra giriş yapın. Spam klasörüne bakmayı unutmayın.";
                return RedirectToAction("Login", "Account");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }
        // GET: /Account/ConfirmEmail
        [HttpGet]
        public async Task<IActionResult> ConfirmEmail(string? userId, string? token)
        {
            var currentCulture = CultureInfo.CurrentCulture.Name;
            var isGerman = currentCulture.StartsWith("de", StringComparison.OrdinalIgnoreCase);
            
            if (userId == null || token == null)
            {
                TempData["Error"] = isGerman 
                    ? "Ungültiger Bestätigungslink." 
                    : "Geçersiz onay bağlantısı.";
                return RedirectToAction(nameof(Login));
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                TempData["Error"] = isGerman 
                    ? "Benutzer nicht gefunden." 
                    : "Kullanıcı bulunamadı.";
                return RedirectToAction(nameof(Login));
            }

            var result = await _userManager.ConfirmEmailAsync(user, token);
            
            if (result.Succeeded)
            {
                _logger.LogInformation("User confirmed email successfully.");
                TempData["Success"] = isGerman 
                    ? "Ihre E-Mail-Adresse wurde erfolgreich bestätigt! Sie können sich jetzt anmelden." 
                    : "E-posta adresiniz başarıyla onaylandı! Artık giriş yapabilirsiniz.";
                TempData["ShowSplash"] = true;
                TempData["SplashMessage"] = isGerman 
                    ? "E-Mail bestätigt! 🎉" 
                    : "E-posta onaylandı! 🎉";
                return RedirectToAction(nameof(Login));
            }
            else
            {
                TempData["Error"] = isGerman 
                    ? "E-Mail-Bestätigung fehlgeschlagen. Der Link ist möglicherweise abgelaufen." 
                    : "E-posta onaylama başarısız oldu. Bağlantının süresi dolmuş olabilir.";
                return RedirectToAction(nameof(Login));
            }
        }

        // POST: /Account/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            _logger.LogInformation("User logged out.");
            return RedirectToAction(nameof(HomeController.Index), "Home");
        }

        private IActionResult RedirectToLocal(string? returnUrl)
        {
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction(nameof(HomeController.Index), "Home");
        }

        // Error view if needed
        public IActionResult Error()
        {
            return View("Error");
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
            var turnstileResult = JsonSerializer.Deserialize<JsonElement>(json);
            return turnstileResult.GetProperty("success").GetBoolean();
        }

    }
}
