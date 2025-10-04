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
        [Authorize]
        public async Task<IActionResult> Quizzes()
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

            // Get the current user.
            var user = await _userManager.GetUserAsync(User);

            // Retrieve all quiz submissions
            var submissions = await _unitOfWork.QuizSubmissions.GetAllAsync();
            var userSubmissions = submissions.Where(s => s.UserId == user!.Id).ToList();

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
                    TeacherName = q.TeacherName ?? "Unknown Instructor",
                    CategoryName = q.Category != null ? q.Category.Name : "General",
                    AttemptCount = userSubmissions.Count(s => s.QuizId == q.Id),
                    LastScore = lastSubmission?.Score,
                    LastAttemptDate = lastSubmission?.SubmissionDate,
                    RecentAttemptDetails = details
                };
            };

            var viewModel = new CombinedQuizzesViewModel
            {
                AvailableQuizzes = allQuizzes.Select(mapQuiz)
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
            ViewBag.RecaptchaSiteKey = _configuration["Recaptcha:SiteKey"];
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        // POST: /Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            if (!string.IsNullOrEmpty(Request.Form["honeypot"]))
            {
                // Honeypot doluysa --> Bot!
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
            var result = await _signInManager.PasswordSignInAsync(user, model.Password, model.RememberMe, false);
            if (result.Succeeded)
            {
                _logger.LogInformation("User logged in.");
                return RedirectToLocal(returnUrl);
            }
            if (result.IsLockedOut)
            {
                _logger.LogWarning("User account locked out.");
                return View("Lockout");
            }


            if (result.Succeeded)
            {
                _logger.LogInformation("User logged in.");
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
            ViewBag.RecaptchaSiteKey = _configuration["Recaptcha:SiteKey"];
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        // POST: /Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            if (!string.IsNullOrEmpty(Request.Form["honeypot"]))
            {
                // Honeypot doluysa --> Bot!
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
            string? recaptchaResponse = Request.Form["g-recaptcha-response"];
            if (!await RecaptchaIsValid(recaptchaResponse ?? ""))
            {
                ModelState.AddModelError("", "Lütfen robot olmadığınızı doğrulayın (reCAPTCHA).");
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
                    userTemplatePath = Path.Combine(Directory.GetCurrentDirectory(), "EmailTemplates", "UserNotification_de.html");
                }
                else
                {
                    userTemplatePath = Path.Combine(Directory.GetCurrentDirectory(), "EmailTemplates", "UserNotification_tr.html");
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
            if (userId == null || token == null)
                return RedirectToAction(nameof(HomeController.Index), "Home");

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound($"Unable to load user with ID '{userId}'.");

            var result = await _userManager.ConfirmEmailAsync(user, token);
            return View(result.Succeeded ? "ConfirmEmail" : "Error");
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
            var recaptchaResult = JsonSerializer.Deserialize<JsonElement>(json);
            return recaptchaResult.GetProperty("success").GetBoolean();
        }

    }
}
