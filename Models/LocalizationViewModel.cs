using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SpeakingClub.Models
{
    public class LocalizationViewModel
    {
    #nullable disable
        public string CurrentLanguage { get; set; } = "de-DE";
        public List<string> AvailableLanguages { get; set; } = new();
        public List<LocalizationModel> Translations { get; set; } = new();
    }
}