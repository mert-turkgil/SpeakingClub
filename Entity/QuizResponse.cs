using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SpeakingClub.Entity
{
    #nullable disable
    public class QuizResponse
    {
        public int QuizResponseId { get; set; }
        public int QuizSubmissionId { get; set; }
        public QuizSubmission QuizSubmission { get; set; }
        
        // Reference to the answer option (if applicable)
        public int? QuizAnswerId { get; set; }
        public QuizAnswer QuizAnswer { get; set; }
        
        // For open-text responses, you might store the answer text.
        public string AnswerText { get; set; }
    }
}
