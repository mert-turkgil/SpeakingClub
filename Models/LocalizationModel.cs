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

    // One text of the site in both languages (Admin → Site metinleri)
    public record SiteTextRow(string Key, string Turkish, string German, string? Comment);
}