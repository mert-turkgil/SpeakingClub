using System.Collections.Generic;
using SpeakingClub.Entity;
using SpeakingClub.Services;

namespace SpeakingClub.Models
{
    #nullable enable
    /// <summary>Who answered a dictionary search, in the order they are asked.</summary>
    public enum DictionarySource { None, Database, DeepL, Pons }

    public class WordViewModel
    {
        public string SearchTerm { get; set; } = string.Empty;

        public DictionarySource Source { get; set; }

        /// <summary>Our own entry (Source = Database). Written by the admins; may contain HTML from the editor.</summary>
        public Word? Word { get; set; }

        /// <summary>Machine translation (Source = DeepL). Plain text.</summary>
        public string? Translation { get; set; }

        /// <summary>Dictionary entry (Source = Pons). Plain text.</summary>
        public PonsEntry? Pons { get; set; }

        /// <summary>Language pair of a DeepL / PONS answer, e.g. "de" → "tr".</summary>
        public string? FromLanguage { get; set; }
        public string? ToLanguage { get; set; }

        /// <summary>Other entries of our dictionary that match the search.</summary>
        public List<Word> Related { get; set; } = new();

        /// <summary>Terms suggested on the empty page (newest entries of our dictionary).</summary>
        public List<string> Suggestions { get; set; } = new();

        public string? WarningMessage { get; set; }

        public bool HasResult => Source != DictionarySource.None;
    }
}
