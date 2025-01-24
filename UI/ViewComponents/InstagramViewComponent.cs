using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace UI.ViewComponents
{
    public class InstagramViewComponent : ViewComponent
    {
        #nullable disable

        public async Task<IViewComponentResult> InvokeAsync()
        {
            // Replace this with actual API call logic
            var instagramData = await GetInstagramData();

            return View(instagramData);
        }

        private async Task<InstagramData> GetInstagramData()
        {
            // Mocked data structure; replace with real API integration logic
            var data = new InstagramData
            {
                Username = "your_instagram_username",
                ProfilePicture = "/images/profile.jpg",
                Stories = new List<string> { "/images/story1.jpg", "/images/story2.jpg" },
                Posts = new List<string> { "/images/post1.jpg", "/images/post2.jpg" }
            };

            // Simulate async operation
            await Task.Delay(100);
            return data;
        }
    

        public class InstagramData
        {
            public string Username { get; set; }
            public string ProfilePicture { get; set; }
            public List<string> Stories { get; set; }
            public List<string> Posts { get; set; }
        }

    }
}