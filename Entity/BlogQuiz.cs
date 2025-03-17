using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace SpeakingClub.Entity
{
    #nullable disable
    public class BlogQuiz
    {
        [ForeignKey("BlogId")]
        public int BlogId { get; set; }
        public Blog Blog { get; set; }

        [ForeignKey("QuizId")]
        public int QuizId { get; set; }
        public Quiz Quiz { get; set; }
    }
}
