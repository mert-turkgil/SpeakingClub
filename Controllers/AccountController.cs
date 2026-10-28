using System;
using System.Globalization;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using SpeakingClub.Data.Abstract;
using SpeakingClub.Entity;
using SpeakingClub.Identity;
using SpeakingClub.Models;
using SpeakingClub.Services;
using SpeakingClub.Services.Email;
using Quiz = SpeakingClub.Entity.Quiz;
using Question = SpeakingClub.Entity.Question;
using QuizAnswer = SpeakingClub.Entity.QuizAnswer;

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
        private readonly EmailQueue _mail;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<AccountController> _logger;
        private readonly ExamService _examService;

        public AccountController(
            IUnitOfWork unitOfWork,
            IConfiguration configuration,
            ILogger<AccountController> logger,
            IMemoryCache memoryCache,
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            EmailQueue mail,
            ExamService examService)
        {
            _examService = examService;
            _configuration = configuration;
            _memoryCache = memoryCache;
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _signInManager = signInManager;
            _mail = mail;
            _logger = logger;
        }
        #region Quiz
        // Online exam flow:
        //   StartQuiz (lobby: rules, pledge) → BeginQuiz (creates or resumes a QuizAttempt) → TakeQuiz (exam room)
        //   → SaveQuizProgress (autosave + integrity events, every few seconds) → SubmitQuiz → ReviewQuiz (result).
        // The server owns the clock: the deadline is fixed when the attempt starts, reloading resumes the attempt,
        // and an attempt left after its deadline is graded from its autosaved answers.

        private IGenericRepository<QuizAttempt> Attempts => _unitOfWork.GenericRepository<QuizAttempt>();

        private bool IsGerman() => CultureInfo.CurrentCulture.TwoLetterISOLanguageName == "de";
        private string T(string tr, string de) => IsGerman() ? de : tr;
        private string QuizzesPath(string? level = null) =>
            (IsGerman() ? "/pruefungen" : "/sinavlar") + (string.IsNullOrWhiteSpace(level) ? "" : "/" + level.Trim().ToLowerInvariant());

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

        // The quiz list lives at /sinavlar (/pruefungen); old links to /Account/Quizzes forward there.
        [AllowAnonymous]
        public IActionResult Quizzes(string? level) => RedirectPermanent(QuizzesPath(level));

        private async Task<QuizAttempt?> GetOwnAttemptAsync(Guid attemptId, string userId) =>
            (await Attempts.GetAllAsync(a => a.Id == attemptId && a.UserId == userId)).FirstOrDefault();

        // The running attempt of this user on this quiz (not submitted, deadline + grace not passed)
        private async Task<QuizAttempt?> FindActiveAttemptAsync(string userId, int quizId)
        {
            var now = DateTime.UtcNow;
            var open = await Attempts.GetAllAsync(a => a.UserId == userId && a.QuizId == quizId && a.SubmittedAt == null);
            return open.Where(a => a.ExpiresAt.AddSeconds(ExamRules.GraceSeconds) > now)
                .OrderByDescending(a => a.StartedAt)
                .FirstOrDefault();
        }

        // Keeps only answers that belong to a question of this quiz
        private static Dictionary<int, int> SanitizeAnswers(Quiz quiz, Dictionary<int, int>? answers)
        {
            var result = new Dictionary<int, int>();
            if (answers == null) return result;
            foreach (var question in quiz.Questions)
            {
                if (answers.TryGetValue(question.Id, out var answerId) && question.Answers.Any(a => a.Id == answerId))
                    result[question.Id] = answerId;
            }
            return result;
        }

        // Id comes from the client so a retried autosave cannot count the same event twice
        private sealed record IntegrityEvent(string Type, DateTime At, string? Id = null);

        private static List<IntegrityEvent> ReadLog(QuizAttempt attempt)
        {
            try
            {
                return string.IsNullOrWhiteSpace(attempt.IntegrityLog)
                    ? new List<IntegrityEvent>()
                    : JsonSerializer.Deserialize<List<IntegrityEvent>>(attempt.IntegrityLog) ?? new List<IntegrityEvent>();
            }
            catch (JsonException)
            {
                return new List<IntegrityEvent>();
            }
        }

        private IActionResult RedirectToResult(QuizAttempt attempt) =>
            RedirectToAction(nameof(ReviewQuiz), new { id = attempt.QuizId, submission = attempt.QuizSubmissionId });

        private string ResultUrl(QuizAttempt attempt) =>
            Url.Action(nameof(ReviewQuiz), new { id = attempt.QuizId, submission = attempt.QuizSubmissionId }) ?? QuizzesPath();

        // Exam lobby (/Account/StartQuiz/5; the old ?id=5 links keep working)
        [Authorize]
        [HttpGet("{id:int?}")]
        public async Task<IActionResult> StartQuiz(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Challenge();

            var quiz = await _unitOfWork.Quizzes.GetForExamAsync(id);
            if (quiz == null)
                return NotFound();

            await _examService.FinalizeExpiredAttemptsAsync(user, quiz);

            var submissions = (await _unitOfWork.QuizSubmissions.GetSubmissionsByUserIdAsync(user.Id))
                .Where(s => s.QuizId == id)
                .OrderByDescending(s => s.SubmissionDate)
                .ToList();
            var active = await FindActiveAttemptAsync(user.Id, id);

            ViewData["Title"] = $"{quiz.Title} | {T("Sınav", "Prüfung")} | Almanca Konuş";
            ViewData["Robots"] = "noindex, nofollow";

            var model = new QuizLobbyViewModel
            {
                QuizId = quiz.Id,
                Title = quiz.Title,
                Description = quiz.Description,
                ImageUrl = quiz.ImageUrl,
                AudioUrl = quiz.AudioUrl,
                YouTubeVideoUrl = ExamRules.VideoEmbedUrl(quiz.YouTubeVideoUrl),
                TeacherName = await GetTeacherDisplayName(quiz.TeacherName),
                CategoryName = quiz.Category?.Name,
                Level = ExamRules.LevelOf(quiz),
                QuestionCount = quiz.Questions.Count,
                TimeLimitMinutes = ExamRules.TimeLimitSeconds(quiz) / 60,
                MaxViolations = ExamRules.MaxViolations,
                AttemptCount = submissions.Count,
                BestScore = submissions.Count > 0 ? submissions.Max(s => s.Score) : null,
                LastScore = submissions.FirstOrDefault()?.Score,
                LastAttemptDate = submissions.FirstOrDefault()?.SubmissionDate,
                ActiveAttemptId = active?.Id,
                ActiveRemainingSeconds = active == null ? 0 : Math.Max(0, (int)(active.ExpiresAt - DateTime.UtcNow).TotalSeconds),
                StudentName = ExamService.FullName(user)
            };
            return View(model);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BeginQuiz(int quizId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Challenge();

            var quiz = await _unitOfWork.Quizzes.GetForExamAsync(quizId);
            if (quiz == null)
                return NotFound();
            if (quiz.Questions.Count == 0)
            {
                TempData["QuizNotice"] = T("Bu sınavda henüz soru yok.", "Diese Prüfung enthält noch keine Fragen.");
                return RedirectToAction(nameof(StartQuiz), new { id = quizId });
            }

            var attempt = await FindActiveAttemptAsync(user.Id, quizId);
            if (attempt == null)
            {
                var now = DateTime.UtcNow;
                attempt = new QuizAttempt
                {
                    Id = Guid.NewGuid(),
                    QuizId = quizId,
                    UserId = user.Id,
                    StartedAt = now,
                    ExpiresAt = now.AddSeconds(ExamRules.TimeLimitSeconds(quiz)),
                    Seed = Random.Shared.Next(),
                    AnswersJson = "{}",
                    IntegrityLog = "[]"
                };
                await Attempts.AddAsync(attempt);
                await _unitOfWork.SaveAsync();
            }
            return RedirectToAction(nameof(TakeQuiz), new { attemptId = attempt.Id });
        }

        // Exam room
        [Authorize]
        [HttpGet("{attemptId:guid}")]
        public async Task<IActionResult> TakeQuiz(Guid attemptId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Challenge();

            var attempt = await GetOwnAttemptAsync(attemptId, user.Id);
            if (attempt == null)
                return NotFound();
            if (attempt.SubmittedAt != null)
                return RedirectToResult(attempt);

            var quiz = await _unitOfWork.Quizzes.GetForExamAsync(attempt.QuizId);
            if (quiz == null)
                return NotFound();

            var now = DateTime.UtcNow;
            if (now > attempt.ExpiresAt.AddSeconds(ExamRules.GraceSeconds))
            {
                var submission = await _examService.FinalizeAttemptAsync(attempt, quiz, ExamService.ReadAnswers(attempt), null, "timeout", user);
                TempData["QuizNotice"] = T("Süre dolduğu için sınavınız kaydedilen cevaplarla otomatik olarak gönderildi.",
                                           "Die Zeit ist abgelaufen – Ihre Prüfung wurde mit den gespeicherten Antworten abgegeben.");
                return RedirectToAction(nameof(ReviewQuiz), new { id = quiz.Id, submission = submission.QuizSubmissionId });
            }

            // Never serve the exam from the browser cache (back button after submitting)
            Response.Headers.CacheControl = "no-store, no-cache, must-revalidate";
            ViewData["Title"] = $"{quiz.Title} | {T("Sınav", "Prüfung")}";
            ViewData["Robots"] = "noindex, nofollow";

            var questions = ExamRules.Shuffle(quiz.Questions.OrderBy(q => q.Id), attempt.Seed)
                .Select((q, i) => new ExamQuestionViewModel
                {
                    Id = q.Id,
                    Number = i + 1,
                    Text = q.QuestionText,
                    ImageUrl = q.ImageUrl,
                    AudioUrl = q.AudioUrl,
                    VideoUrl = ExamRules.VideoEmbedUrl(q.VideoUrl),
                    Answers = ExamRules.Shuffle(q.Answers.OrderBy(a => a.Id), attempt.Seed ^ q.Id)
                        .Select(a => new ExamAnswerViewModel { Id = a.Id, Text = a.AnswerText })
                        .ToList()
                })
                .ToList();

            var model = new QuizExamViewModel
            {
                AttemptId = attempt.Id,
                QuizId = quiz.Id,
                QuizTitle = quiz.Title,
                TeacherName = await GetTeacherDisplayName(quiz.TeacherName),
                StudentName = ExamService.FullName(user),
                ExpiresAtUtc = DateTime.SpecifyKind(attempt.ExpiresAt, DateTimeKind.Utc),
                ServerNowUtc = DateTime.SpecifyKind(now, DateTimeKind.Utc),
                TimeLimitSeconds = (int)(attempt.ExpiresAt - attempt.StartedAt).TotalSeconds,
                ViolationCount = attempt.ViolationCount,
                MaxViolations = ExamRules.MaxViolations,
                Questions = questions,
                SavedAnswers = SanitizeAnswers(quiz, ExamService.ReadAnswers(attempt))
            };
            return View(model);
        }

        public class QuizProgressRequest
        {
            public Guid AttemptId { get; set; }
            public Dictionary<int, int>? Answers { get; set; }
            // Seconds spent on each question so far (questionId → seconds)
            public Dictionary<int, int>? Times { get; set; }
            public List<QuizEventRequest>? Events { get; set; }
        }

        public class QuizEventRequest
        {
            public string? Id { get; set; }
            public string? Type { get; set; }
            public DateTime? At { get; set; }
        }

        // Autosave + integrity events from the exam room (JSON, anti-forgery token in the X-CSRF-TOKEN header)
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveQuizProgress([FromBody] QuizProgressRequest request)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            var attempt = request == null ? null : await GetOwnAttemptAsync(request.AttemptId, user.Id);
            if (attempt == null)
                return NotFound();
            if (attempt.SubmittedAt != null)
                return Json(new { submitted = true, redirect = ResultUrl(attempt) });

            var quiz = await _unitOfWork.Quizzes.GetForExamAsync(attempt.QuizId);
            if (quiz == null)
                return NotFound();

            var now = DateTime.UtcNow;
            var open = now <= attempt.ExpiresAt.AddSeconds(ExamRules.GraceSeconds);
            if (open && request!.Answers != null)
                attempt.AnswersJson = JsonSerializer.Serialize(SanitizeAnswers(quiz, request.Answers));
            if (open && request!.Times != null)
            {
                // Only this quiz's questions, never more than the time allowed
                var limit = ExamRules.TimeLimitSeconds(quiz);
                var questionIds = quiz.Questions.Select(q => q.Id).ToHashSet();
                attempt.TimesJson = JsonSerializer.Serialize(request.Times
                    .Where(t => questionIds.Contains(t.Key))
                    .ToDictionary(t => t.Key, t => Math.Clamp(t.Value, 0, limit)));
            }

            var log = ReadLog(attempt);
            foreach (var e in (request!.Events ?? new List<QuizEventRequest>()).Take(50))
            {
                if (e.Type == null || !ExamRules.EventTypes.Contains(e.Type))
                    continue;
                var eventId = e.Id?.Length > 40 ? e.Id[..40] : e.Id;
                if (eventId != null && log.Any(l => l.Id == eventId))
                    continue;
                log.Add(new IntegrityEvent(e.Type, now, eventId));
                if (ExamRules.ViolationTypes.Contains(e.Type))
                    attempt.ViolationCount++;
            }
            attempt.IntegrityLog = JsonSerializer.Serialize(log.TakeLast(300));

            if (attempt.ViolationCount >= ExamRules.MaxViolations || !open)
            {
                var reason = attempt.ViolationCount >= ExamRules.MaxViolations ? "violations" : "timeout";
                await _examService.FinalizeAttemptAsync(attempt, quiz, ExamService.ReadAnswers(attempt), null, reason, user);
                return Json(new { submitted = true, reason, redirect = ResultUrl(attempt) });
            }

            await _unitOfWork.SaveAsync();
            return Json(new
            {
                ok = true,
                violations = attempt.ViolationCount,
                remaining = Math.Max(0, (int)(attempt.ExpiresAt - now).TotalSeconds),
                serverNow = DateTime.SpecifyKind(now, DateTimeKind.Utc)
            });
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitQuiz(Guid attemptId, Dictionary<int, int>? responses, Dictionary<int, int>? times, string? reason)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Challenge();

            var attempt = await GetOwnAttemptAsync(attemptId, user.Id);
            if (attempt == null)
                return NotFound();
            if (attempt.SubmittedAt != null)
                return RedirectToResult(attempt); // second click, back button, auto-submit racing a manual one

            var quiz = await _unitOfWork.Quizzes.GetForExamAsync(attempt.QuizId);
            if (quiz == null)
                return NotFound();

            // After the deadline (plus grace) only what was autosaved in time counts
            var late = DateTime.UtcNow > attempt.ExpiresAt.AddSeconds(ExamRules.GraceSeconds);
            var answers = late ? SanitizeAnswers(quiz, ExamService.ReadAnswers(attempt)) : SanitizeAnswers(quiz, responses);
            var endReason = late ? "timeout" : reason is "timeout" or "violations" ? reason : "submitted";

            QuizSubmission submission;
            try
            {
                submission = await _examService.FinalizeAttemptAsync(attempt, quiz, answers, times, endReason, user);
            }
            catch (DbUpdateException ex)
            {
                // A concurrent submit of the same attempt won the race (unique AttemptId): show that result
                _logger.LogInformation(ex, "Attempt {AttemptId} was already submitted", attemptId);
                var existing = (await _unitOfWork.QuizSubmissions.GetSubmissionsByUserIdAsync(user.Id))
                    .FirstOrDefault(s => s.AttemptId == attemptId);
                return RedirectToAction(nameof(ReviewQuiz), new { id = attempt.QuizId, submission = existing?.QuizSubmissionId });
            }

            TempData["QuizNotice"] = endReason switch
            {
                "timeout" => T("Süre doldu: sınavınız otomatik olarak gönderildi.", "Die Zeit ist abgelaufen: Ihre Prüfung wurde automatisch abgegeben."),
                "violations" => T("Sınav, kural ihlalleri nedeniyle sonlandırıldı.", "Die Prüfung wurde wegen Regelverstößen beendet."),
                _ => T("Sınavınız başarıyla gönderildi.", "Ihre Prüfung wurde erfolgreich abgegeben.")
            };
            return RedirectToAction(nameof(ReviewQuiz), new { id = quiz.Id, submission = submission.QuizSubmissionId });
        }

        // Result + per-question review of one submission (the latest one when no id is given)
        [Authorize]
        [HttpGet("{id:int?}")]
        public async Task<IActionResult> ReviewQuiz(int id, int? submission)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Challenge();

            var result = submission.HasValue
                ? await _unitOfWork.QuizSubmissions.GetByIdWithResponsesAsync(submission.Value)
                : await _unitOfWork.QuizSubmissions.GetLatestSubmissionByUserAndQuiz(user.Id, id);
            if (result == null || result.UserId != user.Id)
                return NotFound();

            var quiz = await _unitOfWork.Quizzes.GetForExamAsync(result.QuizId);
            var questionsById = quiz?.Questions.ToDictionary(q => q.Id) ?? new Dictionary<int, Question>();

            var details = result.QuizResponses
                .OrderBy(r => r.QuizResponseId)
                .Select((r, i) =>
                {
                    var questionId = r.QuestionId ?? r.QuizAnswer?.QuestionId;
                    var question = questionId.HasValue && questionsById.TryGetValue(questionId.Value, out var q) ? q : null;
                    return new AttemptDetailViewModel
                    {
                        Number = i + 1,
                        QuestionId = question?.Id ?? 0,
                        QuestionText = question?.QuestionText ?? "—",
                        YourAnswer = r.QuizAnswer?.AnswerText,
                        CorrectAnswer = question?.Answers.FirstOrDefault(a => a.IsCorrect)?.AnswerText ?? "—",
                        TimeTakenSeconds = r.TimeTakenSeconds,
                        IsAnswered = r.QuizAnswerId != null,
                        IsCorrect = r.QuizAnswer?.IsCorrect ?? false
                    };
                })
                .ToList();

            ViewData["Title"] = $"{result.Quiz?.Title ?? quiz?.Title} | {T("Sonuç", "Ergebnis")} | Almanca Konuş";
            ViewData["Robots"] = "noindex, nofollow";

            var viewModel = new QuizReviewViewModel
            {
                QuizId = result.QuizId,
                QuizTitle = result.Quiz?.Title ?? quiz?.Title,
                SubmissionDate = result.SubmissionDate,
                Score = result.Score,
                Details = details,
                TotalTimeTaken = result.DurationSeconds > 0 ? result.DurationSeconds : details.Sum(d => d.TimeTakenSeconds),
                SubmissionId = result.QuizSubmissionId,
                AttemptId = result.AttemptId,
                AttemptNumber = result.AttemptNumber,
                CorrectCount = details.Count(d => d.IsCorrect),
                AnsweredCount = details.Count(d => d.IsAnswered),
                TotalQuestions = details.Count,
                ViolationCount = result.ViolationCount,
                EndReason = result.EndReason,
                ImageUrl = quiz?.ImageUrl,
                Level = quiz == null ? null : ExamRules.LevelOf(quiz)
            };

            return View(viewModel);
        }

        #endregion
        #region Account
        // "Hesabım": profile, quiz progress, security and staff shortcuts
        [Authorize]
        public async Task<IActionResult> Account()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                TempData["Error"] = "Unable to load user data.";
                return RedirectToAction("Login", "Account");
            }

            await _examService.FinalizeExpiredAttemptsAsync(user); // exams left running past their deadline
            var submissions = (await _unitOfWork.QuizSubmissions.GetSubmissionsByUserIdAsync(user.Id))
                .OrderBy(s => s.SubmissionDate)
                .ToList();
            var now = DateTime.UtcNow;
            var running = (await Attempts.GetAllAsync(a => a.UserId == user.Id && a.SubmittedAt == null))
                .Where(a => a.ExpiresAt.AddSeconds(ExamRules.GraceSeconds) > now)
                .OrderByDescending(a => a.StartedAt)
                .FirstOrDefault();
            var quizIds = submissions.Select(s => s.QuizId).Append(running?.QuizId ?? 0).Distinct().ToList();
            var titles = (await _unitOfWork.Quizzes.GetAllAsync(q => quizIds.Contains(q.Id)))
                .ToDictionary(q => q.Id, q => IsGerman() && !string.IsNullOrWhiteSpace(q.TitleDe) ? q.TitleDe! : q.Title);
            var roles = await _userManager.GetRolesAsync(user);
            var fullName = ExamService.FullName(user);

            var model = new AccountDashboardViewModel
            {
                FullName = string.IsNullOrWhiteSpace(fullName) ? user.Email ?? "" : fullName,
                Initials = string.Concat(new[] { user.FirstName, user.LastName }
                    .Where(n => !string.IsNullOrWhiteSpace(n)).Select(n => char.ToUpperInvariant(n!.Trim()[0]))),
                Email = user.Email,
                EmailConfirmed = user.EmailConfirmed,
                Age = user.Age,
                Roles = roles,
                IsStaff = roles.Any(r => r is "Root" or "Admin" or "Teacher"),
                CanDelete = !roles.Any(r => r is "Root" or "Admin"),
                HasPassword = await _userManager.HasPasswordAsync(user),
                GoogleLinked = (await _userManager.GetLoginsAsync(user)).Any(l => l.LoginProvider == "Google"),
                NewPostEmails = !user.EmailOptOuts.HasFlag(EmailTopics.NewPosts),
                NewQuizEmails = !user.EmailOptOuts.HasFlag(EmailTopics.NewQuizzes),
                // Signed up with Google (no checkbox there) and never chose: the card asks
                EmailChoicePending = user.PromoConsentSource == ConsentSource.Google,
                TotalAttempts = submissions.Count,
                DistinctQuizzes = submissions.Select(s => s.QuizId).Distinct().Count(),
                ScoreHistory = submissions.TakeLast(12).Select(s => s.Score).ToList(),
                RecentAttempts = submissions.AsEnumerable().Reverse().Take(6).Select(s => new AccountAttemptViewModel
                {
                    QuizId = s.QuizId,
                    SubmissionId = s.QuizSubmissionId,
                    QuizTitle = titles.TryGetValue(s.QuizId, out var title) ? title : "—",
                    Score = s.Score,
                    SubmissionDate = s.SubmissionDate,
                    DurationSeconds = s.DurationSeconds,
                    EndReason = s.EndReason,
                    AttemptNumber = s.AttemptNumber
                }).ToList()
            };
            if (string.IsNullOrEmpty(model.Initials))
                model.Initials = string.IsNullOrEmpty(user.Email) ? "?" : user.Email[..1].ToUpperInvariant();

            if (submissions.Count > 0)
            {
                var average = submissions.Average(s => s.Score);
                model.AverageScore = (int)Math.Round(average);
                model.BestScore = submissions.Max(s => s.Score);
                model.LastScore = submissions[^1].Score;
                model.Trend = submissions.Count > 1 ? submissions[^1].Score - submissions[^2].Score : null;
                // A rough guide from the average score, not a CEFR placement
                model.EstimatedLevel = average >= 90 ? "C2" : average >= 80 ? "C1" : average >= 70 ? "B2"
                    : average >= 60 ? "B1" : average >= 50 ? "A2" : "A1";
            }
            if (running != null)
            {
                model.ActiveExam = new AccountActiveExamViewModel
                {
                    QuizId = running.QuizId,
                    QuizTitle = titles.TryGetValue(running.QuizId, out var runningTitle) ? runningTitle : "—",
                    RemainingSeconds = Math.Max(0, (int)(running.ExpiresAt - now).TotalSeconds)
                };
            }

            ViewData["Robots"] = "noindex, nofollow";
            return View(model);
        }

        // Account page → "E-posta bildirimleri": announcements of new posts and new quizzes (exam results always come)
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EmailPreferences(bool newPosts, bool newQuizzes)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction(nameof(Login));
            user.EmailOptOuts = (newPosts ? EmailTopics.None : EmailTopics.NewPosts) | (newQuizzes ? EmailTopics.None : EmailTopics.NewQuizzes);
            user.PromoConsentAt = DateTime.UtcNow;
            user.PromoConsentSource = ConsentSource.Account;
            await _userManager.UpdateAsync(user);
            TempData["Success"] = T("E-posta tercihleriniz kaydedildi.", "Ihre E-Mail-Einstellungen wurden gespeichert.");
            return Redirect(Url.Action(nameof(Account)) + "#email");
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
                TempData["Success"] = T("Şifreniz başarıyla değiştirildi.", "Ihr Passwort wurde erfolgreich geändert.");
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

            var userId = user!.Id;
            var result = await _userManager.DeleteAsync(user);

            if (result.Succeeded)
            {
                // The download log keeps a copy of the name and e-mail: remove them, as the privacy policy promises.
                // Exam results stay for the statistics, but only with the account id, which no longer points to a person.
                var downloads = await _unitOfWork.GenericRepository<DownloadLog>().GetAllAsync(d => d.UserId == userId);
                foreach (var download in downloads)
                {
                    download.UserEmail = string.Empty;
                    download.UserFullName = string.Empty;
                    _unitOfWork.GenericRepository<DownloadLog>().Update(download);
                }
                await _unitOfWork.SaveAsync();
                await _mail.ForgetUserAsync(userId);   // e-mail log: address and any kept copy

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

                await RecordSignInAsync(user);
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

            var now = DateTime.UtcNow;
            var user = new User
            {
                UserName = model.Email,
                Email = model.Email,
                FirstName = model.FirstName,
                LastName = model.LastName,
                Age = model.Age,
                CreatedAt = now,
                PreferredCulture = UserLanguage.Current,
                // Announcements only with the box ticked; results are sent either way
                EmailOptOuts = model.PromoEmails ? EmailTopics.None : EmailTopics.All,
                PromoConsentAt = model.PromoEmails ? now : null,
                PromoConsentSource = ConsentSource.Register
            };
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

                // Sent by the e-mail worker (Services/Email) in the language of this page; logged on Admin → E-postalar
                await _mail.QueueConfirmEmailAsync(user, confirmationLink!);

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

        #region External login (Google)
        // "Continue with Google": sends the visitor to Google, which returns to ExternalLoginCallback
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExternalLogin(string provider, string? returnUrl = null)
        {
            var schemes = await _signInManager.GetExternalAuthenticationSchemesAsync();
            if (!schemes.Any(s => s.Name == provider))
            {
                TempData["Error"] = T("Google ile giriş henüz etkin değil. Lütfen e-posta ve şifrenizle devam edin.",
                    "Die Anmeldung mit Google ist noch nicht aktiviert. Bitte fahren Sie mit E-Mail und Passwort fort.");
                return RedirectToAction(nameof(Login), new { returnUrl });
            }
            var redirectUrl = Url.Action(nameof(ExternalLoginCallback), "Account", new { returnUrl });
            var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
            return Challenge(properties, provider);
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> ExternalLoginCallback(string? returnUrl = null, string? remoteError = null)
        {
            IActionResult Fail(string tr, string de)
            {
                TempData["Error"] = T(tr, de);
                return RedirectToAction(nameof(Login), new { returnUrl });
            }

            if (remoteError != null)
                return Fail("Google ile giriş iptal edildi veya başarısız oldu.", "Die Anmeldung mit Google wurde abgebrochen oder ist fehlgeschlagen.");

            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
                return Fail("Google bilgileri alınamadı. Lütfen tekrar deneyin.", "Die Google-Daten konnten nicht gelesen werden. Bitte versuchen Sie es erneut.");

            // Someone who has signed in with this Google account before
            var result = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor: true);
            if (result.Succeeded)
            {
                var returning = await _userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey);
                if (returning != null)
                    await RecordSignInAsync(returning);
                return RedirectToLocal(returnUrl);
            }
            if (result.IsLockedOut)
                return View("Lockout");

            var email = info.Principal.FindFirstValue(ClaimTypes.Email);
            if (string.IsNullOrWhiteSpace(email))
                return Fail("Google hesabınız bir e-posta adresi paylaşmadı.", "Ihr Google-Konto hat keine E-Mail-Adresse freigegeben.");

            // Only an address Google has verified may confirm a new account or join an existing one
            if (!string.Equals(info.Principal.FindFirstValue("email_verified"), "true", StringComparison.OrdinalIgnoreCase))
                return Fail("Google hesabınızın e-posta adresi doğrulanmamış.", "Die E-Mail-Adresse Ihres Google-Kontos ist nicht bestätigt.");

            var user = await _userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey)
                       ?? await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                user = new User
                {
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true,
                    FirstName = Shorten(info.Principal.FindFirstValue(ClaimTypes.GivenName)),
                    LastName = Shorten(info.Principal.FindFirstValue(ClaimTypes.Surname)),
                    CreatedAt = DateTime.UtcNow,
                    PreferredCulture = UserLanguage.Current,
                    // No consent box on Google's screens: announcements stay off until chosen on the Account page
                    EmailOptOuts = EmailTopics.All,
                    PromoConsentSource = ConsentSource.Google
                };
                var created = await _userManager.CreateAsync(user);
                if (!created.Succeeded)
                {
                    _logger.LogWarning("Google sign-up failed for {Email}: {Errors}", email, string.Join("; ", created.Errors.Select(e => e.Description)));
                    return Fail("Hesap oluşturulamadı. Lütfen e-posta ile kayıt olmayı deneyin.", "Das Konto konnte nicht erstellt werden. Bitte registrieren Sie sich mit E-Mail.");
                }
                _logger.LogInformation("User created an account with Google.");
            }
            else if (!user.EmailConfirmed)
            {
                user.EmailConfirmed = true; // Google has just proven the address
                await _userManager.UpdateAsync(user);
            }

            if ((await _userManager.GetLoginsAsync(user)).All(l => l.LoginProvider != info.LoginProvider))
            {
                var linked = await _userManager.AddLoginAsync(user, info);
                if (!linked.Succeeded)
                    return Fail("Google hesabı bağlanamadı.", "Das Google-Konto konnte nicht verknüpft werden.");
            }

            await _signInManager.SignInAsync(user, isPersistent: false);
            await RecordSignInAsync(user);
            return RedirectToLocal(returnUrl);
        }
        #endregion

        // Last sign-in and the language the site was used in (member report, language of e-mails)
        // Names from Google fit the column (SpeakingClub.Identity.User.NameMaxLength)
        private static string? Shorten(string? name) =>
            name is { Length: > SpeakingClub.Identity.User.NameMaxLength } ? name[..SpeakingClub.Identity.User.NameMaxLength] : name;

        private async Task RecordSignInAsync(User user)
        {
            user.LastLoginAt = DateTime.UtcNow;
            user.PreferredCulture = UserLanguage.Current;
            await _userManager.UpdateAsync(user);
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
