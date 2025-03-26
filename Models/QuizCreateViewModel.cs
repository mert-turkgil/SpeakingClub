using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;
using SpeakingClub.Services;

namespace SpeakingClub.Models
{
    public class QuizCreateViewModel
    {
        [Required]
        [StringLength(200)]
        public required string Title { get; set; }
        [Required]
        [DataType(DataType.MultilineText)]
        public required string Description { get; set; }

        [Display(Name = "Category")]
        public int? CategoryId { get; set; }

        [Display(Name = "Tags")]
        public List<int> SelectedTagIds { get; set; } = new List<int>();

        [Display(Name = "Associated Words")]
        public List<int> SelectedWordIds { get; set; } = new List<int>();

        public List<QuestionViewModel> Questions { get; set; } = new List<QuestionViewModel>();
        [Display(Name = "Audio File")]
        [DataType(DataType.Upload)]
        [AllowedExtensions(new[] { ".mp3", ".wav", ".ogg" })]
        [MaxFileSize(5 * 1024 * 1024)] // 5MB
        public IFormFile? AudioFile { get; set; }
        // Available options
        public IEnumerable<SelectListItem>? Categories { get; set; }
        public IEnumerable<SelectListItem>? Tags { get; set; }
        public IEnumerable<SelectListItem>? Words { get; set; }
    }
}