using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace SpeakingClub.Services
{
    public class DeeplService : IDeeplService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly ILogger<DeeplService> _logger;
        public DeeplService(HttpClient httpClient, IConfiguration config,
         ILogger<DeeplService> logger)
        {
            _httpClient = httpClient;
            _apiKey = config["DeepL:ApiKey"]??"null";
            _logger = logger;
        }

        public async Task<string?> GetDefinitionAsync(string term)
        {
            try
            {
                // Build the request URL.
                var requestUrl = $"https://api-free.deepl.com/v2/translate?auth_key={_apiKey}&text={Uri.EscapeDataString(term)}&target_lang=DE";

                // Call the API.
                var response = await _httpClient.GetAsync(requestUrl);
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();

                    // Deserialize the JSON result.
                    dynamic? result = JsonConvert.DeserializeObject(json);

                    // Use a local variable for translations to help with nullability checks.
                    var translations = result?.translations;
                    if (translations?.Count > 0)
                    {
                        string definition = Convert.ToString(translations[0].text) ?? string.Empty;
                        return definition;
                    }
                }
                else
                {
                    _logger.LogWarning("DeepL API returned a non-success status code: {StatusCode}", response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                // Log the error along with the term for which the lookup failed.
                _logger.LogError(ex, "Error fetching definition for term '{Term}' from DeepL API", term);
            }
            return null;
        }

        public async Task<string?> GetDefinitionByCultureAsync(string term, string culture)
        {
            try
            {
                // Build the request URL.
                var requestUrl = $"https://api-free.deepl.com/v2/translate?auth_key={_apiKey}&text={Uri.EscapeDataString(term)}&target_lang={culture}";

                // Call the API.
                var response = await _httpClient.GetAsync(requestUrl);
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();

                    // Deserialize the JSON result.
                    dynamic? result = JsonConvert.DeserializeObject(json);

                    // Use a local variable for translations to help with nullability checks.
                    var translations = result?.translations;
                    if (translations?.Count > 0)
                    {
                        string definition = Convert.ToString(translations[0].text) ?? string.Empty;
                        return definition;
                    }
                }
                else
                {
                    _logger.LogWarning("DeepL API returned a non-success status code: {StatusCode}", response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                // Log the error along with the term for which the lookup failed.
                _logger.LogError(ex, "Error fetching definition for term '{Term}' from DeepL API", term);
            }
            return null;
        }
    }
}
