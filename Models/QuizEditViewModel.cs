using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace SpeakingClub.Models
{
    public class QuizEditViewModel
    {
        public int QuizId { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [DataType(DataType.MultilineText)]
        public string Description { get; set; } = string.Empty;

        [Display(Name = "Category")]
        public int? CategoryId { get; set; }

        [Display(Name = "Tags")]
        public List<int> SelectedTagIds { get; set; } = new List<int>();

        [Display(Name = "Associated Words")]
        public List<int> SelectedWordIds { get; set; } = new List<int>();

        public List<QuestionEditViewModel> Questions { get; set; } = new List<QuestionEditViewModel>();

    // Media fields
    [Display(Name = "Audio URL")]
    public string AudioUrl { get; set; } = string.Empty;

    [Display(Name = "YouTube Video URL")]
    public string YouTubeVideoUrl { get; set; } = string.Empty;

    // Teacher information (editable later on the backend). TeacherId will be posted but not shown for editing.
    public string? TeacherId { get; set; }
    [Display(Name = "Teacher")]
    public string TeacherName { get; set; } = string.Empty;
    // Options to allow selecting a teacher when not anonymous
    public IEnumerable<SelectListItem> TeacherOptions { get; set; } = new List<SelectListItem>();

    // Allow making a quiz anonymous
    [Display(Name = "Anonymous")] 
    public bool IsAnonymous { get; set; } = false;

        // Available options
        public IEnumerable<SelectListItem> Categories { get; set; } = new List<SelectListItem>();
        public IEnumerable<SelectListItem> Tags { get; set; } = new List<SelectListItem>();
        public IEnumerable<SelectListItem> Words { get; set; } = new List<SelectListItem>();        
    }
}