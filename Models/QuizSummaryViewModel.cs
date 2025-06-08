using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SpeakingClub.Models
{
    public class QuizSummaryViewModel
    {
        public int QuizId { get; set; }
        public string? QuizTitle { get; set; }
        public string? QuizDescription { get; set; }
        public string? ImageUrl { get; set; }
        public string? TeacherName { get; set; }
        public string? CategoryName { get; set; }
        public List<SpeakingClub.Entity.Tag> Tags { get; set; } = new();
        public string? YouTubeVideoUrl { get; set; }
        // Summary of attempts for the current user:
        public int AttemptCount { get; set; }
        public int? LastScore { get; set; }
        public DateTime? LastAttemptDate { get; set; }
        public string? AudioUrl { get; set; }

        public IEnumerable<AttemptDetailViewModel>? RecentAttemptDetails { get; set; }
    }
}