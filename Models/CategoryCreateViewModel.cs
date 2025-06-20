using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace SpeakingClub.Models
{
    public class CategoryCreateViewModel
    {
        #nullable disable
        [Required, MaxLength(50)]
        public string Name { get; set; }

        // Optional: Assign blogs/quizzes on creation
        public List<int> SelectedBlogIds { get; set; } = new();
        public List<int> SelectedQuizIds { get; set; } = new();

        public IEnumerable<SelectListItem> AvailableBlogs { get; set; } = new List<SelectListItem>();
        public IEnumerable<SelectListItem> AvailableQuizzes { get; set; } = new List<SelectListItem>();
        
    }
}