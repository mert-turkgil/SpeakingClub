using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SpeakingClub.Models
{
    public class BlogResultModel
    {
        #nullable disable
        public int BlogId { get; set; } // Blog ID
        public string Url { get; set; } // Blog URL
        public string TitleUS { get; set; } // English Title
        public string ContentUS { get; set; } // English Content
        public string TitleTR { get; set; } // Turkish Title
        public string ContentTR { get; set; } // Turkish Content
        public string TitleDE { get; set; } // German Title
        public string ContentDE { get; set; } // German Content
    }
}