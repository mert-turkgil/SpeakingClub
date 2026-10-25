using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SpeakingClub.Models
{
    public class QuizReviewViewModel
    {
        public int QuizId { get; set; }
        public string? QuizTitle { get; set; }
        public DateTime SubmissionDate { get; set; }
        public int Score { get; set; }
        public required IEnumerable<AttemptDetailViewModel> Details { get; set; }
        public int TotalTimeTaken { get; set; }

        public int SubmissionId { get; set; }
        public Guid? AttemptId { get; set; }
        public int AttemptNumber { get; set; }
        public int CorrectCount { get; set; }
        public int AnsweredCount { get; set; }
        public int TotalQuestions { get; set; }
        public int ViolationCount { get; set; }
        public string? EndReason { get; set; }
        public string? ImageUrl { get; set; }
        public string? Level { get; set; }
    }
}