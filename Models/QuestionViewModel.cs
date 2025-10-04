using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using SpeakingClub.Services;

namespace SpeakingClub.Models
{
    public class QuestionViewModel
    {
        public int Id { get; set; }
        public string QuestionText { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public string? AudioUrl { get; set; }
        public string? VideoUrl { get; set; }
        public string? QuizTitle { get; set; } // For display purposes when showing available questions
        
        // File upload properties
        [Display(Name = "Image File")]
        [DataType(DataType.Upload)]
        [AllowedExtensions(new[] { ".jpg", ".jpeg", ".png", ".gif" })]
        [MaxFileSize(5 * 1024 * 1024)] // 5MB
        public IFormFile? ImageFile { get; set; }
        
        [Display(Name = "Audio File")]
        [DataType(DataType.Upload)]
        [AllowedExtensions(new[] { ".mp3", ".wav", ".ogg", ".m4a" })]
        [MaxFileSize(10 * 1024 * 1024)] // 10MB
        public IFormFile? AudioFile { get; set; }
        
        [ValidateAtLeastOneCorrectAnswer]
        public List<AnswerViewModel> Answers { get; set; } = new List<AnswerViewModel>();
    }
}