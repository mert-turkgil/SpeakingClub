using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SpeakingClub.Models
{
    public class CombinedQuizzesViewModel
    {
        public IEnumerable<QuizSummaryViewModel>? AvailableQuizzes  { get; set; }

        // Level from /sinavlar/{level}, pre-selected in the filters
        public string? SelectedLevel { get; set; }

        // The signed-in user's progress (all zero for visitors)
        public int TotalAttempts { get; set; }
        public int AverageScore { get; set; }
        public int BestScore { get; set; }
        public int CompletedQuizzes { get; set; }
    }
}