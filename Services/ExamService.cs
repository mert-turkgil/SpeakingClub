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

        public ExamService(IUnitOfWork unitOfWork, IHubContext<QuizMonitorHub> hubContext, ILogger<ExamService> logger)
        {
            _unitOfWork = unitOfWork;
            _hubContext = hubContext;
            _logger = logger;
        }

        private IGenericRepository<QuizAttempt> Attempts => _unitOfWork.GenericRepository<QuizAttempt>();

        public static string FullName(User user) =>
            string.Join(" ", new[] { user.FirstName, user.LastName }.Where(s => !string.IsNullOrWhiteSpace(s))).Trim();

        public static Dictionary<int, int> ReadAnswers(QuizAttempt attempt)
        {
            try
            {
                return string.IsNullOrWhiteSpace(attempt.AnswersJson)
                    ? new Dictionary<int, int>()
                    : JsonSerializer.Deserialize<Dictionary<int, int>>(attempt.AnswersJson) ?? new Dictionary<int, int>();
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
            var previousAttempts = (await _unitOfWork.QuizSubmissions.GetSubmissionsByUserIdAsync(attempt.UserId))
                .Count(s => s.QuizId == quiz.Id);

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
            foreach (var question in questions)
            {
                QuizAnswer? chosen = answers.TryGetValue(question.Id, out var answerId)
                    ? question.Answers.FirstOrDefault(a => a.Id == answerId)
                    : null;
                if (chosen?.IsCorrect == true)
                    correct++;
                var spent = times != null && times.TryGetValue(question.Id, out var seconds) ? Math.Clamp(seconds, 0, duration) : 0;
                submission.QuizResponses.Add(new QuizResponse
                {
                    QuestionId = question.Id,
                    QuizAnswerId = chosen?.Id,
                    AnswerText = chosen == null ? "No answer" : null,
                    TimeTakenSeconds = spent
                });
            }
            submission.Score = ExamRules.Percent(correct, questions.Count);

            await _unitOfWork.QuizSubmissions.AddAsync(submission);
            attempt.SubmittedAt = now;
            attempt.AnswersJson = JsonSerializer.Serialize(answers);
            await _unitOfWork.SaveAsync();
            attempt.QuizSubmissionId = submission.QuizSubmissionId;
            await _unitOfWork.SaveAsync();

            // Live update for teachers on /QuizMonitor (same fields as QuizMonitorViewModel)
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
    }
}
