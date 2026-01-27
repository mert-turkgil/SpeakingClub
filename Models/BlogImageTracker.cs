using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SpeakingClub.Models
{
    public class BlogImageTracker
    {
        public int BlogId { get; set; }
        public string UrlSlug { get; set; } = "";
        public DateTime LastUpdated { get; set; }
        public HashSet<string> Images { get; set; } = new();

        public BlogImageTracker() { }
        
        public BlogImageTracker(int blogId, string urlSlug)
        {
            BlogId = blogId;
            UrlSlug = urlSlug;
            LastUpdated = DateTime.UtcNow;
        }

        public void AddImage(string imagePath)
        {
            Images.Add(imagePath);
            LastUpdated = DateTime.UtcNow;
        }

        public IEnumerable<string> GetAllImages() => Images;
    }
}