using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SpeakingClub.Entity
{
    #nullable disable
    public class Question
    {
        public Question()
        {
            Answers = new List<QuizAnswer>();
        }

        public int Id { get; set; }

        // The text of the question (optional if using media)
        public string QuestionText { get; set; }

        // Media URLs (optional, allowing multimedia questions)
        public string ImageUrl { get; set; }
        public string AudioUrl { get; set; }
        public string VideoUrl { get; set; }

        // Foreign key to the Quiz
        public int QuizId { get; set; }
        public Quiz Quiz { get; set; }

        // Navigation to answers
        public ICollection<QuizAnswer> Answers { get; set; }
    }
}