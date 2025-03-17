using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
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

        // URL or path to an MP3/sound file.
        public string? AudioUrl { get; set; }

        // Optional YouTube video URL for the quiz question.
        public string? YouTubeVideoUrl { get; set; }

        // The teacher (user) who created the quiz.
        public string? TeacherId { get; set; }
        public User? Teacher { get; set; }

        public ICollection<QuizAnswer>? Answers { get; set; }
    }
}