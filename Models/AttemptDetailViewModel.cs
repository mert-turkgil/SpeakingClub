using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SpeakingClub.Models
{
    public class AttemptDetailViewModel
    {
        public int QuestionId { get; set; }
        public string? QuestionText { get; set; }
        public string? YourAnswer { get; set; }
        public string? CorrectAnswer { get; set; }
        public int TimeTakenSeconds { get; set; }
        public bool IsCorrect { get; set; }
    }
}