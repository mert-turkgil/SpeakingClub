using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace SpeakingClub.Models
{
    // WordCreateViewModel.cs
    public class WordCreateViewModel
    {
        [Required]
        [StringLength(100, ErrorMessage = "Term cannot exceed 100 characters")]
        public required string Term { get; set; }

        [Required]
        public string? Definition { get; set; }

        [DataType(DataType.MultilineText)]
        public string? Example { get; set; }

        [Display(Name = "Pronunciation Guide")]
        public string? Pronunciation { get; set; }

        [Display(Name = "Synonyms (comma separated)")]
        public string? Synonyms { get; set; }

        public string? Origin { get; set; }

        [Display(Name = "API Imported")]
        public bool IsFromApi { get; set; }

        [Display(Name = "Related Quizzes")]
        public List<int> SelectedQuizIds { get; set; } = new List<int>();

        public IEnumerable<SelectListItem>? AvailableQuizzes { get; set; }
    }
}