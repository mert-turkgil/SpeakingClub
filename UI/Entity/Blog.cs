using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace UI.Entity
{
    public class Blog
    {
        #nullable disable
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; }

        [Required]
        public string Content { get; set; } // Stores HTML content from CKEditor

        public DateTime DateCreated { get; set; } = DateTime.Now;

        public DateTime? DateModified { get; set; }

        public string Author { get; set; }

        //
                
        public string ImageUrl { get; set; } // Optional for image paths
        public string VideoUrl { get; set; } // Optional for video paths
        
    }
}