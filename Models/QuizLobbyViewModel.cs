using System;

namespace SpeakingClub.Models
{
    // The exam lobby: what the quiz is, the rules, and the user's previous attempts
    public class QuizLobbyViewModel
    {
        public int QuizId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public string? AudioUrl { get; set; }
        public string? YouTubeVideoUrl { get; set; }
        public string TeacherName { get; set; } = string.Empty;
        public string? CategoryName { get; set; }
        public string? Level { get; set; }

        public int QuestionCount { get; set; }
        public int TimeLimitMinutes { get; set; }
        public int MaxViolations { get; set; }

        public int AttemptCount { get; set; }
        public int? BestScore { get; set; }
        public int? LastScore { get; set; }
        public DateTime? LastAttemptDate { get; set; }

        // An attempt that is still running: the lobby offers "Resume" with the time left
        public Guid? ActiveAttemptId { get; set; }
        public int ActiveRemainingSeconds { get; set; }

        public string StudentName { get; set; } = string.Empty;
    }
}
