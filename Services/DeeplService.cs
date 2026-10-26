using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace SpeakingClub.Services
{
    /// <summary>
    /// DeepL API v2 (https://developers.deepl.com/docs/api-reference/translate).
    /// The key goes in the Authorization header (query-string keys are no longer accepted),
    /// and free keys (ending in ":fx") use api-free.deepl.com, paid keys api.deepl.com.
    /// </summary>
    public class DeeplService : IDeeplService
    {
        private readonly HttpClient _httpClient;
        private readonly string? _apiKey;
        private readonly ILogger<DeeplService> _logger;

        public DeeplService(HttpClient httpClient, IConfiguration config, ILogger<DeeplService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            var key = config["DeepL:ApiKey"]?.Trim();
            _apiKey = string.IsNullOrEmpty(key) || key == "null" ? null : key;
        }

        public bool IsConfigured => _apiKey != null;

        private string Endpoint => _apiKey!.EndsWith(":fx", StringComparison.Ordinal)
            ? "https://api-free.deepl.com/v2/translate"
            : "https://api.deepl.com/v2/translate";

        public async Task<DeeplTranslation?> TranslateAsync(string text, string targetLang, string? sourceLang = null, CancellationToken cancellationToken = default)
        {
            if (!IsConfigured || string.IsNullOrWhiteSpace(text)) return null;

            var form = new List<KeyValuePair<string, string>>
            {
                new("text", text),
                new("target_lang", TargetCode(targetLang))
            };
            if (!string.IsNullOrEmpty(sourceLang))
                form.Add(new("source_lang", sourceLang.ToUpperInvariant()));

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, Endpoint)
                {
                    Content = new FormUrlEncodedContent(form)
                };
                request.Headers.Authorization = new AuthenticationHeaderValue("DeepL-Auth-Key", _apiKey);

                using var response = await _httpClient.SendAsync(request, cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    // 403 = wrong key, 456 = monthly character quota used up
                    _logger.LogWarning("DeepL returned {StatusCode} for target {Target}", (int)response.StatusCode, targetLang);
                    return null;
                }

                await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                using var json = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
                if (!json.RootElement.TryGetProperty("translations", out var translations) || translations.GetArrayLength() == 0)
                    return null;

                var first = translations[0];
                var translated = first.GetProperty("text").GetString();
                if (string.IsNullOrWhiteSpace(translated)) return null;
                var detected = first.TryGetProperty("detected_source_language", out var d) ? d.GetString() : sourceLang;
                return new DeeplTranslation(translated.Trim(), (detected ?? "").ToLowerInvariant(), targetLang.ToLowerInvariant());
            }
            catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "DeepL request failed for target {Target}", targetLang);
                return null;
            }
        }

        // DeepL wants a regional variant for English and Portuguese targets
        private static string TargetCode(string lang) => lang.ToLowerInvariant() switch
        {
            "en" => "EN-GB",
            "pt" => "PT-PT",
            var other => other.ToUpperInvariant()
        };
    }
}
