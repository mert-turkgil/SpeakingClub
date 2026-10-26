using System.Threading;
using System.Threading.Tasks;

namespace SpeakingClub.Services
{
    /// <summary>One DeepL translation: the text and the language DeepL detected (two-letter, lower case).</summary>
    public record DeeplTranslation(string Text, string SourceLanguage, string TargetLanguage);

    public interface IDeeplService
    {
        /// <summary>False when no API key is configured; lookups are then skipped.</summary>
        bool IsConfigured { get; }

        /// <summary>
        /// Translates <paramref name="text"/> into <paramref name="targetLang"/> ("tr", "de", "en"…).
        /// Without <paramref name="sourceLang"/> DeepL detects the language itself.
        /// Returns null when nothing came back or the API failed (the error is logged).
        /// </summary>
        Task<DeeplTranslation?> TranslateAsync(string text, string targetLang, string? sourceLang = null, CancellationToken cancellationToken = default);
    }
}
