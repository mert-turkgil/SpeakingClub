using System.ComponentModel.DataAnnotations;
using SpeakingClub.Identity;

namespace SpeakingClub.Entity
{
    #nullable disable
    public class UserQuiz
    {
        // These two properties can serve as a composite key (configured via Fluent API)
        public string UserId { get; set; }
        public User User { get; set; }
        
        public int QuizId { get; set; }
        public Quiz Quiz { get; set; }
        
        // Optional: track total attempts for this user on the quiz
        public int TotalAttempts { get; set; }
    }
}
