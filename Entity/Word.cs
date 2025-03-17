using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace SpeakingClub.Entity
{
    #nullable disable
    public class Word
    {
        public int WordId { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string Term { get; set; }
        
        public string Definition { get; set; }
        public string Example { get; set; }
        public string Pronunciation { get; set; }

        public string Synonyms { get; set; }

        public string Origin { get; set; }
        
        // Indicates whether this entry was created locally or fetched via an API.
        public bool IsFromApi { get; set; }
        
        // New: Many-to-many relationship with Quiz
        public ICollection<Quiz> Quizzes { get; set; } = new List<Quiz>();
    }
}
