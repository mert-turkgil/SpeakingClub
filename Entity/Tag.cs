using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SpeakingClub.Entity
{
    #nullable disable
    public class Tag
    {
        public int TagId { get; set; }
        
        [Required]
        [MaxLength(50)]
        public string Name { get; set; }
        
        // Many-to-many relationships with content types
        public ICollection<Blog> Blogs { get; set; } = new List<Blog>();
        public ICollection<Quiz> Quizzes { get; set; } = new List<Quiz>();
    }
}
