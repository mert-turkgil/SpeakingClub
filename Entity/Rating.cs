using System;
using System.ComponentModel.DataAnnotations;
using SpeakingClub.Identity;

namespace SpeakingClub.Entity
{
    #nullable disable
    public class Rating
    {
        public int RatingId { get; set; }
        
        [Range(1, 5)]
        public int Score { get; set; }
        
        public DateTime Date { get; set; }
        
        // Optional: link the rating to a Blog or Quiz
        public int? BlogId { get; set; }
        public Blog Blog { get; set; }
        
        public int? QuizId { get; set; }
        public Quiz Quiz { get; set; }
        
        // The user who gave the rating
        public string UserId { get; set; }
        public User User { get; set; }
    }
}
