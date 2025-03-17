using System;
using System.ComponentModel.DataAnnotations;
using SpeakingClub.Identity;

namespace SpeakingClub.Entity
{
    #nullable disable
    public class Comment
    {
        public int CommentId { get; set; }
        
        [Required]
        public string Content { get; set; }
        
        public DateTime Date { get; set; }
        
        // Optional: link the comment to a Blog or Quiz
        public int? BlogId { get; set; }
        public Blog Blog { get; set; }
        
        public int? QuizId { get; set; }
        public Quiz Quiz { get; set; }
        
        // The user who made the comment
        public string UserId { get; set; }
        public User User { get; set; }
    }
}
