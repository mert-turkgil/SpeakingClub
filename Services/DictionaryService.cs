using System;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SpeakingClub.Models;

namespace SpeakingClub.Services
{
    public class DictionaryService : IDictionaryService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly ILogger<DictionaryService> _logger;

        public DictionaryService(HttpClient httpClient, IConfiguration config, ILogger<DictionaryService> logger)
        {
            _httpClient = httpClient;
            _apiKey = config["Pons:ApiKey"] ?? throw new ArgumentNullException(nameof(config), "PONS API Key is missing");
            _logger = logger;
        }

        public async Task<WordViewModel?> GetWordDetailsAsync(string word, string sourceLang, string targetLang)
        {
            try
            {
                // Construct the dictionary code: the documentation shows the code is the concatenation
                // of the two-letter codes, e.g. for an English-to-German lookup, targetLang = "de" and sourceLang = "en"
                // produces "deen".
                string dictionaryCode = $"{targetLang}{sourceLang}";
                string requestUrl = $"https://api.pons.com/v1/dictionary?q={Uri.EscapeDataString(word)}&l={dictionaryCode}";

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("X-Secret", _apiKey);

                var response = await _httpClient.GetAsync(requestUrl);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("PONS API returned an error: {StatusCode}", response.StatusCode);
                    return null;
                }

                var jsonResponse = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrWhiteSpace(jsonResponse) || response.StatusCode == System.Net.HttpStatusCode.NoContent)
                {
                    return null;
                }

                // Deserialize using our updated model classes
                var ponsResults = JsonSerializer.Deserialize<PonsResponse[]>(jsonResponse, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                var firstResult = ponsResults?.FirstOrDefault();
                if (firstResult == null || firstResult.hits == null || firstResult.hits.Length == 0)
                    return null;

                var firstHit = firstResult.hits.FirstOrDefault();
                if (firstHit == null || firstHit.roms == null || firstHit.roms.Length == 0)
                    return null;

                var firstRom = firstHit.roms.FirstOrDefault();
                if (firstRom == null || firstRom.arabs == null || firstRom.arabs.Length == 0)
                    return null;

                var firstArab = firstRom.arabs.FirstOrDefault();
                if (firstArab == null || firstArab.translations == null || firstArab.translations.Length == 0)
                    return null;

                var firstTranslation = firstArab.translations.FirstOrDefault();

                return new WordViewModel
                {
                    SearchTerm = word,
                    Word = new Entity.Word
                    {
                        // You can choose to use the headword from the response or simply the original search term.
                        Term = firstRom.headword ?? word,
                        // Use the target of the first translation as the definition.
                        Definition = firstTranslation?.target ?? "No definition available",
                        // Since PONS doesn't provide these in this response, they are left empty.
                        Pronunciation = string.Empty,
                        Example = string.Empty,
                        Synonyms = string.Empty,
                        Origin = string.Empty,
                        IsFromApi = true
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching definition for word {Word} from PONS API", word);
                return null;
            }
        }

        // Updated response model classes matching the JSON returned by PONS

        private class PonsResponse
        {
            public string? lang { get; set; } 
            public Hit[] hits { get; set; } = Array.Empty<Hit>();
        }

        private class Hit
        {
            public string? type { get; set; }
            public bool opendict { get; set; }
            public Rom[] roms { get; set; } = Array.Empty<Rom>();
        }

        private class Rom
        {
            public string headword { get; set; } = "";
            public string headword_full { get; set; } = "";
            public string wordclass { get; set; } = "";
            public Arab[] arabs { get; set; } = Array.Empty<Arab>();
        }

        private class Arab
        {
            public string header { get; set; } = "";
            public Translation[] translations { get; set; } = Array.Empty<Translation>();
        }

        private class Translation
        {
            public string source { get; set; } = "";
            public string target { get; set; } = "";
        }
    }
}
