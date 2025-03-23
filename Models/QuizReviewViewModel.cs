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
    }
}