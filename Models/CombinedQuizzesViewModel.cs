using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SpeakingClub.Models
{
    public class CombinedQuizzesViewModel
    {
        public IEnumerable<QuizSummaryViewModel>? AvailableQuizzes  { get; set; }
    }
}