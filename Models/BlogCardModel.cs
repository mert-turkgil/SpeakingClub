using System;

namespace SpeakingClub.Models
{
    // A blog post as shown in a card (blog list, related posts), already resolved to one language.
    public class BlogCardModel
    {
        public int BlogId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Excerpt { get; set; } = string.Empty;
        public string Href { get; set; } = string.Empty; // site-relative URL of the post, e.g. /yazilar/my-post
        public string Image { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string? CategoryName { get; set; }
        public DateTime Date { get; set; }
        public int ReadingMinutes { get; set; }
    }
}
