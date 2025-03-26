using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace SpeakingClub.Models
{
    public class AnswerViewModel
    {
        [Required]
        public required string AnswerText { get; set; }= string.Empty;
        public bool IsCorrect { get; set; }
    }
}