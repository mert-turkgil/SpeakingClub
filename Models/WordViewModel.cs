using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SpeakingClub.Entity;

namespace SpeakingClub.Models
{
    #nullable enable
    public class WordViewModel
    {
        public string SearchTerm { get; set; } = string.Empty;

        public Word? Word { get; set; }

        public string? WarningMessage { get; set; } = string.Empty;
    }

}