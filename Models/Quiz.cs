using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using SpeakingClub.Entity;
using SpeakingClub.Identity;

namespace SpeakingClub.Models
{
    #nullable enable
    public class Quiz
    {
        public int Id { get; set; }

        [Required]
        public string? Title { get; set; }
        public string? Description { get; set; }

        // Optional audio or video resources
        public string? AudioUrl { get; set; }
        public string? YouTubeVideoUrl { get; set; }

        // The teacher who created the quiz
        public string? TeacherId { get; set; }
        public User? Teacher { get; set; }

        // ✅ New: Each quiz has multiple questions
        public ICollection<Question>? Questions { get; set; }
        public int? CategoryId { get; set; }
        public virtual ICollection<Tag> Tags { get; set; } = new List<Tag>();
        public virtual ICollection<Word> Words { get; set; } = new List<Word>();
    }
}