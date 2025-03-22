using System;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
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
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly IEmailSender _emailSender;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            IUnitOfWork unitOfWork,
            ILogger<AccountController> logger,
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            IEmailSender emailSender)
        {
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
            var quizzes = await _unitOfWork.Quizzes.GetAllAsync();
            return View(quizzes);
        }

        [Authorize]
        public async Task<IActionResult> StartQuiz(int id)
        {
            var quiz = await _unitOfWork.Quizzes.GetByIdAsync(id);
            if (quiz == null)
                return NotFound();

            return View(quiz);
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> SubmitQuiz(int QuizId, Dictionary<int, int> responses, int ElapsedTime)
        {
            var user = await _userManager.GetUserAsync(User);
            var quiz = await _unitOfWork.Quizzes.GetByIdAsync(QuizId);

            if (quiz == null)
                return NotFound();

            var submission = new QuizSubmission
            {
                UserId = user!.Id,
                QuizId = QuizId,
                SubmissionDate = DateTime.UtcNow,
                Score = 0, // initial score
                AttemptNumber = 1 // handle logic accordingly
            };

            int score = 0;

            foreach (var question in quiz.Questions)
            {
                var selectedAnswerId = responses.ContainsKey(question.Id) ? responses[question.Id] : (int?)null;
                var correctAnswer = question.Answers.FirstOrDefault(a => a.IsCorrect);

                if (selectedAnswerId == correctAnswer?.Id)
                    score += 1;

                var quizResponse = new QuizResponse
                {
                    QuizAnswerId = selectedAnswerId,
                    AnswerText = selectedAnswerId == null ? "No answer" : null
                };

                submission.QuizResponses.Add(quizResponse);
            }

            submission.Score = (int)((double)score / quiz.Questions.Count * 100);
            await _unitOfWork.QuizSubmissions.AddAsync(submission);
            await _unitOfWork.SaveAsync();

            TempData["Success"] = $"Quiz completed! Your score is {submission.Score}% (Time: {ElapsedTime}s)";
            return RedirectToAction(nameof(Quizzes));
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
            var userSubmissions = submissions.Where(qs => qs.UserId == user.Id).ToList();

            // Check if any quizzes have been completed
            bool hasQuizData = userSubmissions.Any();
            ViewBag.HasQuizData = hasQuizData;

            if (hasQuizData)
            {
                double averageScore = userSubmissions.Average(q => q.Score);
                string level;

                    if (averageScore >= 90)
                    {
                        level = "C2";
                    }
                    else if (averageScore >= 80)
                    {
                        level = "C1";
                    }
                    else if (averageScore >= 70)
                    {
                        level = "B2";
                    }
                    else if (averageScore >= 60)
                    {
                        level = "B1";
                    }
                    else if (averageScore >= 50)
                    {
                        level = "A2";
                    }
                    else
                    {
                        level = "A1";
                    }


                ViewBag.UserLevel = level;
                ViewBag.Progress = averageScore;
            }
            else
            {
                ViewBag.UserLevel = null;
                ViewBag.Progress = 0;
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
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        // POST: /Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

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
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        // POST: /Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (!ModelState.IsValid)
                return View(model);

            var user = new User { UserName = model.Email, Email = model.Email };
            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                _logger.LogInformation("User created a new account.");

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
                if (currentCulture.StartsWith("en", StringComparison.OrdinalIgnoreCase))
                {
                    userTemplatePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "EmailTemplates", "UserNotification_en.html");
                }
                else if (currentCulture.StartsWith("de", StringComparison.OrdinalIgnoreCase))
                {
                    userTemplatePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "EmailTemplates", "UserNotification_de.html");
                }
                else
                {
                    userTemplatePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "EmailTemplates", "UserNotification_tr.html");
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
                await _signInManager.SignInAsync(user, isPersistent: false);
                return RedirectToLocal(returnUrl);
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
    }
}
