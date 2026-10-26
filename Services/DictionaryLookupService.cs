using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using SpeakingClub.Data.Abstract;
using SpeakingClub.Data.Concrete;
using SpeakingClub.Models;

namespace SpeakingClub.Services
{
    /// <summary>
    /// The /sozluk search. Sources are asked in a fixed order and the first answer wins:
    ///   1. our own dictionary (Admin → Kelimeler), case- and article-insensitive;
    ///   2. DeepL, Turkish ⇄ German;
    ///   3. PONS German–Turkish dictionary.
    /// DeepL and PONS answers are cached, so a popular word costs one API call per 12 hours.
    /// </summary>
    public class DictionaryLookupService
    {
        public const int MaxTermLength = 100;   // = Word.Term max length; also keeps long texts away from DeepL
        private const int RelatedCount = 6;
        private static readonly TimeSpan AnswerLifetime = TimeSpan.FromHours(12);
        private static readonly TimeSpan MissLifetime = TimeSpan.FromMinutes(10);

        private readonly IUnitOfWork _unitOfWork;
        private readonly IDeeplService _deepl;
        private readonly IDictionaryService _pons;
        private readonly IMemoryCache _cache;

        public DictionaryLookupService(IUnitOfWork unitOfWork, IDeeplService deepl, IDictionaryService pons, IMemoryCache cache)
        {
            _unitOfWork = unitOfWork;
            _deepl = deepl;
            _pons = pons;
            _cache = cache;
        }

        /// <summary>A cached DeepL / PONS answer.</summary>
        private sealed record ExternalAnswer(DictionarySource Source, string? Translation, PonsEntry? Pons, string From, string To);

        /// <param name="uiLanguage">"tr" or "de": the page language decides the translation direction.</param>
        public async Task LookupAsync(WordViewModel model, string uiLanguage, CancellationToken cancellationToken = default)
        {
            var term = WordRepository.NormalizeTerm(model.SearchTerm);
            if (term.Length == 0) return;
            var ui = uiLanguage == "de" ? "de" : "tr";

            // 1. Our own dictionary
            var word = await _unitOfWork.Words.GetWordByTermAsync(term);
            if (word != null && !string.IsNullOrWhiteSpace(word.Definition))
            {
                model.Source = DictionarySource.Database;
                model.Word = word;
            }
            model.Related = await _unitOfWork.Words.GetRelatedAsync(term, model.Word?.WordId, RelatedCount);
            if (model.HasResult) return;

            // 2. DeepL, 3. PONS
            var cacheKey = "dictionary:" + ui + ":" + term.ToLowerInvariant();
            if (!_cache.TryGetValue(cacheKey, out ExternalAnswer? answer))
            {
                answer = await AskDeeplAsync(term, ui, cancellationToken) ?? await AskPonsAsync(term, cancellationToken);
                _cache.Set(cacheKey, answer, answer == null ? MissLifetime : AnswerLifetime);
            }
            if (answer == null) return;

            model.Source = answer.Source;
            model.Translation = answer.Translation;
            model.Pons = answer.Pons;
            model.FromLanguage = answer.From;
            model.ToLanguage = answer.To;
        }

        public Task<List<string>> SuggestionsAsync(int count) => _unitOfWork.Words.GetLatestTermsAsync(count);

        private async Task<ExternalAnswer?> AskDeeplAsync(string term, string ui, CancellationToken cancellationToken)
        {
            if (!_deepl.IsConfigured) return null;

            // First into the page language, letting DeepL detect what was typed
            var result = await _deepl.TranslateAsync(term, ui, null, cancellationToken);
            if (result == null) return null;

            if (result.SourceLanguage == ui)
            {
                // Typed in the page language → into the other one (Turkish ⇄ German)
                result = await _deepl.TranslateAsync(term, ui == "de" ? "tr" : "de", ui, cancellationToken);
            }
            else if (result.SourceLanguage is not ("de" or "tr"))
            {
                // Single words are often detected as English or Dutch ("Kind" → "tür", "Hand");
                // this is a German dictionary, so read them as German
                result = await _deepl.TranslateAsync(term, "tr", "de", cancellationToken);
            }

            // DeepL echoes what it cannot translate (names, typos) — let PONS try instead
            if (result == null || string.Equals(result.Text.Trim(' ', '.'), term, StringComparison.OrdinalIgnoreCase))
                return null;
            return new ExternalAnswer(DictionarySource.DeepL, result.Text, null, result.SourceLanguage, result.TargetLanguage);
        }

        private async Task<ExternalAnswer?> AskPonsAsync(string term, CancellationToken cancellationToken)
        {
            if (!_pons.IsConfigured) return null;
            var entry = await _pons.LookupAsync(term, null, cancellationToken);
            if (entry == null) return null;
            var from = entry.Language == "tr" ? "tr" : "de";
            return new ExternalAnswer(DictionarySource.Pons, null, entry, from, from == "de" ? "tr" : "de");
        }
    }
}
