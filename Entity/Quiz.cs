using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using SpeakingClub.Identity;

namespace SpeakingClub.Entity
{
    #nullable disable
    public class Quiz
    {
        public Quiz()
        {
            Questions = new List<Question>();
            Blogs = new HashSet<Blog>();
            Tags = new List<Tag>();
            Words = new List<Word>();
            UserQuizzes = new List<UserQuiz>();
        }
        public int Id { get; set; }

        [Required]
        public string Title { get; set; }
        public string Description { get; set; }

        // URL or path to an MP3/sound file.
        public string AudioUrl { get; set; }

        // Optional YouTube video URL for the quiz question.
        public string YouTubeVideoUrl { get; set; }

        public string ImageUrl { get; set; }

        // The teacher (user) who created the quiz.
        public string TeacherId { get; set; }
        public User Teacher { get; set; }

        public ICollection<Question> Questions { get; set; }

        public ICollection<QuizAnswer> Answers { get; set; }
        // One-to-many relationship with Blog
        public virtual ICollection<Blog> Blogs { get; set; } 
        
        // New: Category relationship (optional)
        public int? CategoryId { get; set; }
        public Category Category { get; set; }
        
        // New: Many-to-many relationship with Tags
        public ICollection<Tag> Tags { get; set; }
        
        // New: Many-to-many relationship with Word for vocabulary-based quizzes
        public ICollection<Word> Words { get; set; }
        
        // New: Direct relationship with users through a join table (UserQuiz)
        public ICollection<UserQuiz> UserQuizzes { get; set; }
    }
}
