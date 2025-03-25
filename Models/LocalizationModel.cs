using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SpeakingClub.Models
{
    public class LocalizationModel
    {
        public string Key { get; set; } = string.Empty;  // Translation key
        public string Value { get; set; } = string.Empty;  // Translated text
        public string Comment { get; set; } = string.Empty; 
    }
}