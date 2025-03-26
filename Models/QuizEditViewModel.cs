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

        // Available options
        public IEnumerable<SelectListItem> Categories { get; set; } = new List<SelectListItem>();
        public IEnumerable<SelectListItem> Tags { get; set; } = new List<SelectListItem>();
        public IEnumerable<SelectListItem> Words { get; set; } = new List<SelectListItem>();        
    }
}