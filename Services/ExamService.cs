using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using SpeakingClub.Data.Abstract;
using SpeakingClub.Entity;
using SpeakingClub.Hubs;
using SpeakingClub.Identity;
using SpeakingClub.Services.Email;

namespace SpeakingClub.Services
{
    /// <summary>
    /// Grades exam attempts. Used by the exam pages (AccountController) and by the quiz list (HomeController),
    /// which collects attempts whose deadline passed while the student was away.
    /// </summary>
    public class ExamService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHubContext<QuizMonitorHub> _hubContext;
        private readonly ILogger<ExamService> _logger;
        private readonly EmailQueue _mail;

        public ExamService(IUnitOfWork unitOfWork, IHubContext<QuizMonitorHub> hubContext, ILogger<ExamService> logger, EmailQueue mail)
        {
            _unitOfWork = unitOfWork;
            _hubContext = hubContext;
            _logger = logger;
            _mail = mail;
        }

        private IGenericRepository<QuizAttempt> Attempts => _unitOfWork.GenericRepository<QuizAttempt>();

        public static string FullName(User user) =>
            string.Join(" ", new[] { user.FirstName, user.LastName }.Where(s => !string.IsNullOrWhiteSpace(s))).Trim();

        public static Dictionary<int, int> ReadAnswers(QuizAttempt attempt) => ReadMap(attempt.AnswersJson);

        // Seconds per question as last autosaved (questionId → seconds)
        public static Dictionary<int, int> ReadTimes(QuizAttempt attempt) => ReadMap(attempt.TimesJson);

        private static Dictionary<int, int> ReadMap(string? json)
        {
            try
            {
                return string.IsNullOrWhiteSpace(json)
                    ? new Dictionary<int, int>()
                    : JsonSerializer.Deserialize<Dictionary<int, int>>(json) ?? new Dictionary<int, int>();
            }
            catch (JsonException)
            {
                return new Dictionary<int, int>();
            }
        }

        /// <summary>
        /// Grades an attempt and stores it as a QuizSubmission (once: the unique AttemptId index makes a second
        /// call a no-op). Responses keep their question, so unanswered questions appear in the review.
        /// </summary>
        public async Task<QuizSubmission> FinalizeAttemptAsync(QuizAttempt attempt, Quiz quiz, Dictionary<int, int> answers,
            Dictionary<int, int>? times, string reason, User user)
        {
            var now = DateTime.UtcNow;
            var end = now < attempt.ExpiresAt ? now : attempt.ExpiresAt;
            var duration = Math.Max(0, (int)(end - attempt.StartedAt).TotalSeconds);
            var previousAttempts = await _unitOfWork.QuizSubmissions.CountAsync(s => s.UserId == attempt.UserId && s.QuizId == quiz.Id);
            // Collected without the page's final timings (time ran out, rules ended it): use the autosaved ones
            times ??= ReadTimes(attempt);

            var submission = new QuizSubmission
            {
                UserId = attempt.UserId,
                QuizId = quiz.Id,
                SubmissionDate = now,
                AttemptNumber = previousAttempts + 1,
                AttemptId = attempt.Id,
                StartedAt = attempt.StartedAt,
                DurationSeconds = duration,
                ViolationCount = attempt.ViolationCount,
                EndReason = reason
            };

            // Responses in the order the student saw the questions
            var questions = ExamRules.Shuffle(quiz.Questions.OrderBy(q => q.Id), attempt.Seed);
            var correct = 0;
            var answered = 0;
            foreach (var question in questions)
            {
                QuizAnswer? chosen = answers.TryGetValue(question.Id, out var answerId)
                    ? question.Answers.FirstOrDefault(a => a.Id == answerId)
                    : null;
                if (chosen?.IsCorrect == true)
                    correct++;
                if (chosen != null)
                    answered++;
                var spent = times.TryGetValue(question.Id, out var seconds) ? Math.Clamp(seconds, 0, duration) : 0;
                submission.QuizResponses.Add(new QuizResponse
                {
                    QuestionId = question.Id,
                    QuizAnswerId = chosen?.Id,
                    AnswerText = chosen == null ? "No answer" : null,
                    TimeTakenSeconds = spent,
                    IsCorrect = chosen?.IsCorrect == true
                });
            }
            submission.Score = ExamRules.Percent(correct, questions.Count);
            submission.TimesRecorded = times.Count > 0;

            await _unitOfWork.QuizSubmissions.AddAsync(submission);
            attempt.SubmittedAt = now;
            attempt.AnswersJson = JsonSerializer.Serialize(answers);
            await _unitOfWork.SaveAsync();
            attempt.QuizSubmissionId = submission.QuizSubmissionId;
            await _unitOfWork.SaveAsync();   // ReportCacheInterceptor refreshes the teachers' reports

            // The result by e-mail too (sent by EmailOutboxService; never fails the grading)
            await _mail.QueueExamResultAsync(submission, quiz, user, correct, questions.Count, answered);

            // Live update for teachers on /QuizMonitor: the page shows a "new result" notice and refreshes its figures
            try
            {
                await _hubContext.Clients.All.SendAsync("ReceiveSubmission", new
                {
                    SubmissionId = submission.QuizSubmissionId,
                    UserName = FullName(user),
                    QuizTitle = quiz.Title,
                    Score = submission.Score,
                    MaxScore = 100,
                    AttemptNumber = submission.AttemptNumber,
                    SubmissionDate = submission.SubmissionDate,
                    SubmissionTimeFormatted = submission.SubmissionDate.ToString("g"),
                    Age = user.Age,
                    DurationSeconds = submission.DurationSeconds,
                    ViolationCount = submission.ViolationCount,
                    EndReason = submission.EndReason,
                    Responses = Array.Empty<object>()
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not broadcast quiz submission {SubmissionId}", submission.QuizSubmissionId);
            }

            return submission;
        }

        /// <summary>
        /// Attempts whose deadline passed without a submission (browser closed, connection lost) are graded
        /// from what was autosaved, like an exam that is collected when time is up. All quizzes, or only one.
        /// </summary>
        public async Task FinalizeExpiredAttemptsAsync(User user, Quiz? quiz = null)
        {
            var now = DateTime.UtcNow;
            var expired = (await Attempts.GetAllAsync(a => a.UserId == user.Id && a.SubmittedAt == null))
                .Where(a => (quiz == null || a.QuizId == quiz.Id) && a.ExpiresAt.AddSeconds(ExamRules.GraceSeconds) <= now)
                .ToList();
            foreach (var attempt in expired)
            {
                var attemptQuiz = quiz ?? await _unitOfWork.Quizzes.GetForExamAsync(attempt.QuizId);
                if (attemptQuiz != null)
                    await FinalizeAttemptAsync(attempt, attemptQuiz, ReadAnswers(attempt), null, "timeout", user);
            }
        }

        /// <summary>
        /// Every student's expired attempts (EmailOutboxService, every few minutes): graded like above, so the result
        /// e-mail goes out without the student having to come back. At most <paramref name="max"/> per call.
        /// </summary>
        public async Task<int> FinalizeAllExpiredAsync(Func<string, Task<User?>> findUser, int max = 25)
        {
            var cutoff = DateTime.UtcNow.AddSeconds(-ExamRules.GraceSeconds);
            var expired = (await Attempts.GetAllAsync(a => a.SubmittedAt == null && a.ExpiresAt <= cutoff))
                .OrderBy(a => a.ExpiresAt).Take(max).ToList();
            var collected = 0;
            foreach (var attempt in expired)
            {
                var user = await findUser(attempt.UserId);
                var quiz = await _unitOfWork.Quizzes.GetForExamAsync(attempt.QuizId);
                if (user == null || quiz == null)
                {
                    // Account or quiz deleted meanwhile: close the attempt so it is not picked up again
                    attempt.SubmittedAt = DateTime.UtcNow;
                    await _unitOfWork.SaveAsync();
                    continue;
                }
                await FinalizeAttemptAsync(attempt, quiz, ReadAnswers(attempt), null, "timeout", user);
                collected++;
            }
            return collected;
        }
    }
}
