using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using UI.Data.Abstract;
using UI.Data.Concrete;
using UI.Models;

namespace UI.ViewComponents
{
    public class CardViewComponent : ViewComponent
    {
        #nullable disable
        private static List<string> _cachedImages;
        private static DateTime _lastFetched = DateTime.MinValue;
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
            List<string> images = await GetUnsplashImagesAsync(3);
            Console.WriteLine("------------------------------------------------");
            string list = string.Join(",",images);
            Console.WriteLine(list);
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

            // Caching Mechanism
            if (_cachedImages != null && (DateTime.Now - _lastFetched).TotalMinutes < 30)
            {
                return _cachedImages;
            }

            var baseUrl = "https://api.unsplash.com/";
            var requestUrl = $"{baseUrl}photos/random?count={count}&query=textures-patterns&client_id={accessKey}";

            try
            {
                // Create a list to hold all the image URLs
                List<string> imageUrls = new List<string>();
                List<UnsplashImage> photos = await _httpClient.GetFromJsonAsync<List<UnsplashImage>>(requestUrl);
                foreach (var photo in photos)
                {
                    imageUrls.Add(photo.Urls.Full);
                }

                return imageUrls;
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
        public Urls Urls { get; set; }
    }

    public class Urls
    {
        public string Thumb { get; set; }
        public string Small { get; set; }
        public string Regular { get; set; }
        public string Full { get; set; }
    }
}
