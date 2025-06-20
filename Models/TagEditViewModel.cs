using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace SpeakingClub.Models
{
    public class TagEditViewModel
    {
        #nullable disable
        [Key]
        public int TagId { get; set; }
        [Required, MaxLength(50)]
        public string Name { get; set; }

        // For multi-select
        public List<int> SelectedBlogIds { get; set; } = new();
        public List<int> SelectedQuizIds { get; set; } = new();

        // For displaying available options
        public IEnumerable<SelectListItem> AvailableBlogs { get; set; } = new List<SelectListItem>();
        public IEnumerable<SelectListItem> AvailableQuizzes { get; set; } = new List<SelectListItem>();
     
    }
}