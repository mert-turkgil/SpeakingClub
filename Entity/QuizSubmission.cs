using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SpeakingClub.Identity;

namespace SpeakingClub.Entity
{
    #nullable disable
    public class QuizSubmission
    {
        public int QuizSubmissionId { get; set; }
        public int QuizId { get; set; }
        public Quiz Quiz { get; set; }
        public string UserId { get; set; }
        public User User { get; set; }
        public DateTime SubmissionDate { get; set; }
        // Optional: a computed score or result field
        public int Score { get; set; }
        
        // New: Attempt counter if a user can attempt the quiz multiple times
        public int AttemptNumber { get; set; }
    }
}
