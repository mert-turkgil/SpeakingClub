using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace SpeakingClub.Entity
{
    [Keyless]
    public class QuizAnalysis
    {
        public int QuizId { get; set; }
        public int TotalSubmissions { get; set; }
        public double AverageScore { get; set; }
    }
}
