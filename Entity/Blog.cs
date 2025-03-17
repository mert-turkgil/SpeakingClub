using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using SpeakingClub.Models;

namespace SpeakingClub.Entity
{
    public class Blog
    {
        #nullable disable
        public Blog()
        {
            Quiz = new HashSet<Quiz>();
            Tags = new List<Tag>();
        }

        [Key]
        public int BlogId { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        public string Content { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string Image { get; set; } = string.Empty;

        public DateTime Date { get; set; }

        [MaxLength(100)]
        public string Author { get; set; } = string.Empty;

        public string RawYT { get; set; } = string.Empty;
        public string RawMaps { get; set; } = string.Empty;

        public bool isHome { get; set; }

        // One-to-Many relationship with Quiz
        public virtual ICollection<Quiz> Quiz { get; set; }

        // New: Category relationship (optional)
        public int? CategoryId { get; set; }
        public Category Category { get; set; }
        
        // New: Many-to-many relationship with Tags
        public ICollection<Tag> Tags { get; set; }
    }
}
