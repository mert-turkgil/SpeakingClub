using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace SpeakingClub.Models
{
    public class CategoryEditViewModel
    {
        #nullable disable
        [Key]
        public int CategoryId { get; set; }

        [Required, MaxLength(50)]
        public string Name { get; set; }

        // Multi-select for Blogs and Quizzes
        public List<int> SelectedBlogIds { get; set; } = new();
        public List<int> SelectedQuizIds { get; set; } = new();

        // Display all available Blogs/Quizzes for selection
        public IEnumerable<SelectListItem> AvailableBlogs { get; set; } = new List<SelectListItem>();
        public IEnumerable<SelectListItem> AvailableQuizzes { get; set; } = new List<SelectListItem>();
        
    }
}