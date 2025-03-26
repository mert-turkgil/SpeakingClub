using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using SpeakingClub.Services;

namespace SpeakingClub.Models
{
    public class QuestionViewModel
    {
        [Required]
        public required string QuestionText { get; set; }
        public string? ImageUrl { get; set; }
        public string? AudioUrl { get; set; }
        public string? VideoUrl { get; set; }
        
        [ValidateAtLeastOneCorrectAnswer]
        public List<AnswerViewModel> Answers { get; set; } = new List<AnswerViewModel>();
    }
}