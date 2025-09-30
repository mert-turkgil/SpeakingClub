using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace SpeakingClub.Models
{
    public class AnswerEditViewModel
    {
        public int AnswerId { get; set; }

        [Required]
        public string AnswerText { get; set; } = string.Empty;
        public string IsCorrect { get; set; } = "false";
    
    }
}