using System;
using System.Collections.Generic;

namespace SpeakingClub.Models
{
    // The exam room for one attempt. Questions and answers are already in this attempt's (seeded) order;
    // correct answers are never sent to the browser.
    public class QuizExamViewModel
    {
        public Guid AttemptId { get; set; }
        public int QuizId { get; set; }
        public string QuizTitle { get; set; } = string.Empty;
        public string TeacherName { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;

        public DateTime ExpiresAtUtc { get; set; }
        public DateTime ServerNowUtc { get; set; }
        public int TimeLimitSeconds { get; set; }

        public int ViolationCount { get; set; }
        public int MaxViolations { get; set; }

        public List<ExamQuestionViewModel> Questions { get; set; } = new();
        // Answers autosaved earlier in this attempt (restored after a reload): questionId → answerId
        public Dictionary<int, int> SavedAnswers { get; set; } = new();
    }

    public class ExamQuestionViewModel
    {
        public int Id { get; set; }
        public int Number { get; set; }
        public string? Text { get; set; }
        public string? ImageUrl { get; set; }
        public string? AudioUrl { get; set; }
        public string? VideoUrl { get; set; }
        public List<ExamAnswerViewModel> Answers { get; set; } = new();
    }

    public class ExamAnswerViewModel
    {
        public int Id { get; set; }
        public string Text { get; set; } = string.Empty;
    }
}
