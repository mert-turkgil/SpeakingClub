using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using SpeakingClub.Identity;

namespace SpeakingClub.Entity
{
    #nullable disable
    public class Article
    {
        public int ArticleId { get; set; }
        
        [Required]
        [MaxLength(200)]
        public string Title { get; set; }
        
        public string Content { get; set; }
        public DateTime Date { get; set; }
        
        // Teacher association (teacher is a role using UserId)
        public string TeacherId { get; set; }
        public User Teacher { get; set; }
        
        public string Url { get; set; }
        public string Image { get; set; }
        
        // New: Category relationship (optional)
        public int? CategoryId { get; set; }
        public Category Category { get; set; }
        
        // New: Many-to-many relationship with Tags
        public ICollection<Tag> Tags { get; set; } = new List<Tag>();
    }
}
