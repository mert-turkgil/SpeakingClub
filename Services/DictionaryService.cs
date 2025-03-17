using System;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SpeakingClub.Entity;
using SpeakingClub.Models;

namespace SpeakingClub.Services
{
    public class DictionaryService : IDictionaryService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<DictionaryService> _logger;
        private readonly IDeeplService _deeplService;


        public DictionaryService(HttpClient httpClient, ILogger<DictionaryService> logger, IDeeplService deeplService)
        {
            _httpClient = httpClient;
            _logger = logger;
            _deeplService = deeplService;
        }

        public async Task<WordViewModel?> GetWordDetailsAsync(string word)
        {
            var currentCulture = CultureInfo.CurrentCulture.Name;
            var langCode = currentCulture.Substring(0, 2).ToLower();
            try
            {
                var response = await _httpClient.GetFromJsonAsync<DictionaryApiResponse[]>($"https://api.dictionaryapi.dev/api/v2/entries/de/{word}");

                var result = response?.FirstOrDefault();

                if (result == null)
                    return null;

                var definition = result.meanings?.FirstOrDefault()?.definitions?.FirstOrDefault();
                var translatedDefinition = await _deeplService.GetDefinitionByCultureAsync(result.word, langCode);
                return new WordViewModel
                {
                    SearchTerm = word,
                    Word = new Entity.Word
                    {
                        Term = result.word,
                        Definition = definition?.definition ?? "Definition unavailable",
                        Example = definition?.example ?? "Example unavailable",
                        Pronunciation = result.phonetics?.FirstOrDefault()?.text ?? "",
                        Origin = result.origin ?? "Origin unavailable",
                        Synonyms = string.Join(", ", definition?.synonyms ?? Array.Empty<string>()),
                        IsFromApi = true
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,$"Error fetching definition for word {word}", word);
                return null;
            }
        }

        // Helper classes to deserialize JSON
        private class DictionaryApiResponse
        {
            public string word { get; set; } = "";
            public Phonetic[] phonetics { get; set; } = Array.Empty<Phonetic>();
            public Meaning[] meanings { get; set; } = Array.Empty<Meaning>();
            public string? origin { get; set; }
        }

        private class Phonetic
        {
            public string? text { get; set; }
            public string? audio { get; set; }
        }

        private class Meaning
        {
            public string? partOfSpeech { get; set; }
            public Definition[] definitions { get; set; } = Array.Empty<Definition>();
        }

        private class Definition
        {
            public string? definition { get; set; }
            public string? example { get; set; }
            public string[] synonyms { get; set; } = Array.Empty<string>();
        }
    }
}
