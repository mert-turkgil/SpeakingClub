using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace SpeakingClub.Entity
{
    #nullable disable
    public class QuizAnswer
    {
        public int Id { get; set; }

        // Foreign key to the Question
        public int QuestionId { get; set; }
        public Question Question { get; set; }

        [Required]
        public string AnswerText { get; set; }

        // Only one answer per question should be true
        public bool IsCorrect { get; set; }
    }
}
