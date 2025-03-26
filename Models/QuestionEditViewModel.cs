using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using SpeakingClub.Services;

namespace SpeakingClub.Models
{
    public class QuestionEditViewModel
    {
        public int QuestionId { get; set; }

        [Required]
        public string QuestionText { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public string? AudioUrl { get; set; }
        public string? VideoUrl { get; set; }
        
        [ValidateAtLeastOneCorrectAnswer]
        public List<AnswerEditViewModel> Answers { get; set; } = new List<AnswerEditViewModel>();
    }
}