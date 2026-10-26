using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace SpeakingClub.Services
{
    /// <summary>
    /// PONS dictionary API (https://en.pons.com/p/files/uploads/pons/api/api-documentation.pdf).
    /// Always asks the German–Turkish dictionary ("detr"); the old code built "dede" on the German
    /// site, which PONS does not have. PONS returns small HTML fragments, which are turned into
    /// plain text here so the page never has to render them as HTML.
    /// </summary>
    public class DictionaryService : IDictionaryService
    {
        private const string DictionaryCode = "detr";
        private const int MaxMeanings = 8;
        private static readonly Regex Tags = new("<[^>]+>", RegexOptions.Compiled);
        private static readonly Regex Spaces = new(@"\s+", RegexOptions.Compiled);

        private readonly HttpClient _httpClient;
        private readonly string? _apiKey;
        private readonly ILogger<DictionaryService> _logger;

        public DictionaryService(HttpClient httpClient, IConfiguration config, ILogger<DictionaryService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            // A missing key used to throw here, which broke every page of HomeController
            var key = config["Pons:ApiKey"]?.Trim();
            _apiKey = string.IsNullOrEmpty(key) || key == "null" ? null : key;
        }

        public bool IsConfigured => _apiKey != null;

        public async Task<PonsEntry?> LookupAsync(string term, string? sourceLang = null, CancellationToken cancellationToken = default)
        {
            if (!IsConfigured || string.IsNullOrWhiteSpace(term)) return null;

            var url = $"https://api.pons.com/v1/dictionary?q={Uri.EscapeDataString(term)}&l={DictionaryCode}";
            if (sourceLang is "de" or "tr") url += "&in=" + sourceLang;

            try
            {
                // Per-request header: HttpClient.DefaultRequestHeaders is shared and not thread-safe
                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("X-Secret", _apiKey);

                using var response = await _httpClient.SendAsync(request, cancellationToken);
                if (response.StatusCode == HttpStatusCode.NoContent) return null; // no entry
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("PONS returned {StatusCode}", (int)response.StatusCode);
                    return null;
                }

                await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                var results = await JsonSerializer.DeserializeAsync<PonsLanguage[]>(stream,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }, cancellationToken);
                return ToEntry(term, results);
            }
            catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "PONS request failed");
                return null;
            }
        }

        private static PonsEntry? ToEntry(string term, PonsLanguage[]? results)
        {
            var language = results?.FirstOrDefault(r => r.Hits.Length > 0);
            if (language == null) return null;

            string? headword = null;
            string? wordClass = null;
            var meanings = new List<PonsMeaning>();

            foreach (var hit in language.Hits)
            {
                // "translation" hits carry the pair directly, "entry" hits nest it in roms → arabs
                if (hit.Source != null && hit.Target != null)
                    Add(meanings, hit.Source, hit.Target);

                foreach (var rom in hit.Roms)
                {
                    headword ??= Plain(rom.Headword);
                    wordClass ??= string.IsNullOrWhiteSpace(rom.Wordclass) ? null : rom.Wordclass;
                    foreach (var translation in rom.Arabs.SelectMany(a => a.Translations))
                        Add(meanings, translation.Source, translation.Target);
                }
            }

            if (meanings.Count == 0) return null;
            return new PonsEntry(string.IsNullOrEmpty(headword) ? term : headword, wordClass, language.Lang ?? "", meanings.Take(MaxMeanings).ToList());
        }

        private static void Add(List<PonsMeaning> meanings, string source, string target)
        {
            var meaning = new PonsMeaning(Plain(source), Plain(target));
            if (meaning.Target.Length == 0) return;
            if (meanings.Any(m => string.Equals(m.Target, meaning.Target, StringComparison.OrdinalIgnoreCase))) return;
            meanings.Add(meaning);
        }

        /// <summary>"&lt;strong class=headword&gt;Hand&lt;/strong&gt; &lt;span&gt;f&lt;/span&gt;" → "Hand f"</summary>
        private static string Plain(string? html) =>
            string.IsNullOrEmpty(html) ? "" : Spaces.Replace(WebUtility.HtmlDecode(Tags.Replace(html, " ")), " ").Trim();

        // Response shape: [{ lang, hits: [{ type, source?, target?, roms: [{ headword, wordclass, arabs: [{ translations: [{ source, target }] }] }] }] }]
        private sealed class PonsLanguage
        {
            public string? Lang { get; set; }
            public PonsHit[] Hits { get; set; } = Array.Empty<PonsHit>();
        }

        private sealed class PonsHit
        {
            public string? Type { get; set; }
            public string? Source { get; set; }
            public string? Target { get; set; }
            public PonsRom[] Roms { get; set; } = Array.Empty<PonsRom>();
        }

        private sealed class PonsRom
        {
            public string? Headword { get; set; }
            public string? Wordclass { get; set; }
            public PonsArab[] Arabs { get; set; } = Array.Empty<PonsArab>();
        }

        private sealed class PonsArab
        {
            public PonsTranslation[] Translations { get; set; } = Array.Empty<PonsTranslation>();
        }

        private sealed class PonsTranslation
        {
            public string Source { get; set; } = "";
            public string Target { get; set; } = "";
        }
    }
}
