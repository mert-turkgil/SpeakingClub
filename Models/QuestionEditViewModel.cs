using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;
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

        public int QuizId { get; set; }

        [ValidateAtLeastOneCorrectAnswer]
        public List<AnswerEditViewModel> Answers { get; set; } = new List<AnswerEditViewModel>();
        public IEnumerable<SelectListItem>? AvailableQuizzes { get; set; }
        
        public IFormFile? ImageFile { get; set; }
        public IFormFile? AudioFile { get; set; }
    }
}