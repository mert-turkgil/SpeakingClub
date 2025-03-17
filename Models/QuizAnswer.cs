using System.ComponentModel.DataAnnotations;

namespace SpeakingClub.Models
{
    #nullable disable
    public class QuizAnswer
    {
        public int Id { get; set; }
        
        // Foreign key to the Quiz.
        public int QuizId { get; set; }
        public Quiz Quiz { get; set; }

        [Required]
        public string AnswerText { get; set; }

        // Only one answer should have this flag set to true.
        public bool IsCorrect { get; set; }
    }
}
