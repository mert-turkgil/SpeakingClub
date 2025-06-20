using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SpeakingClub.Models
{
    public class QuizMonitorViewModel
    {
        #nullable disable
        public int SubmissionId { get; set; }
        public string UserName { get; set; }
        public string QuizTitle { get; set; }
        public int Score { get; set; }
        public int MaxScore { get; set; }
        public int AttemptNumber { get; set; }
        public DateTime SubmissionDate { get; set; }
        public string SubmissionTimeFormatted { get; set; }
        public int Age { get; set; }
        public List<UserResponseViewModel> Responses { get; set; } // optional, for drilldown
        
    }
}