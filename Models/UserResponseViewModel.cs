using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SpeakingClub.Models
{
    public class UserResponseViewModel
    {
        #nullable disable
        public string Question { get; set; }
        public string Answer { get; set; }
        public bool IsCorrect { get; set; }
        public int TimeTakenSeconds { get; set; }        
    }
}