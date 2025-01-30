using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using UI.Data.Abstract;
using UI.Data.Concrete;
using UI.Models;

namespace UI.ViewComponents
{
    public class CardViewComponent : ViewComponent
    {
        #nullable disable
        private readonly IBlogRepository _blogRepository;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        private readonly ILogger<CardViewComponent> _logger;

        public CardViewComponent(IConfiguration configuration, ILogger<CardViewComponent> logger,IBlogRepository blogRepository)
        {
            _blogRepository = blogRepository;
            _configuration = configuration;
            _httpClient = new HttpClient();
            _logger = logger;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var images = await GetUnsplashImagesAsync(3);
            var blogs = await _blogRepository.GetAllAsync();
            
            var model = new CardModel
            {
                Images = images,
                BlogCount = blogs.Count,
                InstaPosts = 0 // Sonra ilgileneceğim
            };
            
            return View(model);
        }

        private async Task<List<string>> GetUnsplashImagesAsync(int count)
        {
            var accessKey = _configuration["Unsplash:AccessKey"];
            if (string.IsNullOrEmpty(accessKey))
            {
                _logger.LogError("Unsplash Access Key is missing.");
                return new List<string>(); // Return an empty list
            }

            var baseUrl = "https://api.unsplash.com/";
            var requestUrl = $"{baseUrl}photos/random?count={count}&query=textures-patterns&client_id={accessKey}";

            try
            {
                var response = await _httpClient.GetAsync(requestUrl);
                
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"Unsplash API request failed: {response.StatusCode}");
                    return new List<string>(); // Return an empty list
                }

                var json = await response.Content.ReadAsStringAsync();
                var images = JsonSerializer.Deserialize<List<UnsplashImage>>(json);
                
                // Filter out null or empty URLs
                var validUrls = images?
                    .Select(img => img.Urls?.Regular)
                    .Where(url => !string.IsNullOrEmpty(url))
                    .ToList();

                return validUrls ?? new List<string>(); // Return valid URLs or an empty list
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception when calling Unsplash API: {ex.Message}");
                return new List<string>(); // Return an empty list
            }
        }
    }

    public class UnsplashImage
    {
        public UnsplashUrls Urls { get; set; }
    }

    public class UnsplashUrls
    {
        public string Regular { get; set; }
    }
}
