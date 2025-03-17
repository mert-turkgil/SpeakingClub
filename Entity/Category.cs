using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SpeakingClub.Entity
{
    #nullable disable
    public class Category
    {
        public int CategoryId { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string Name { get; set; }
        
        // One-to-many relationships with content types
        public ICollection<Article> Articles { get; set; } = new List<Article>();
        public ICollection<Blog> Blogs { get; set; } = new List<Blog>();
        public ICollection<Quiz> Quizzes { get; set; } = new List<Quiz>();
    }
}
