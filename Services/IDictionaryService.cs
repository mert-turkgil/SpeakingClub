using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SpeakingClub.Services
{
    /// <summary>One line of a PONS entry, already turned into plain text: "Hand" → "el".</summary>
    public record PonsMeaning(string Source, string Target);

    /// <summary>A PONS dictionary entry. <see cref="Language"/> is the language of the headword ("de" or "tr").</summary>
    public record PonsEntry(string Headword, string? WordClass, string Language, IReadOnlyList<PonsMeaning> Meanings);

    /// <summary>PONS German ⇄ Turkish dictionary — the last fallback of the /sozluk page.</summary>
    public interface IDictionaryService
    {
        /// <summary>False when no API key is configured; lookups are then skipped.</summary>
        bool IsConfigured { get; }

        /// <summary>
        /// Looks <paramref name="term"/> up in the PONS German–Turkish dictionary.
        /// <paramref name="sourceLang"/> ("de"/"tr") helps PONS when the language is known.
        /// Returns null when PONS has no entry or the API failed (the error is logged).
        /// </summary>
        Task<PonsEntry?> LookupAsync(string term, string? sourceLang = null, CancellationToken cancellationToken = default);
    }
}
