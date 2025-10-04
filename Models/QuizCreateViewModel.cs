using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
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
        // Available options
    public IEnumerable<SelectListItem>? Categories { get; set; }
    public IEnumerable<SelectListItem>? Tags { get; set; }
    public IEnumerable<SelectListItem>? Words { get; set; }
    
    // Available questions to select from
    public List<QuestionViewModel>? AvailableQuestions { get; set; }
// Media fields (optional URLs)
[Display(Name = "Audio URL")]
public string? AudioUrl { get; set; }    [Display(Name = "YouTube Video URL")]
    public string? YouTubeVideoUrl { get; set; }

    // Teacher selection (frontend only for now)
    public string? TeacherId { get; set; }
    public string TeacherName { get; set; } = string.Empty;
    public IEnumerable<SelectListItem> TeacherOptions { get; set; } = new List<SelectListItem>();
    public bool IsAnonymous { get; set; } = false;
    }
}