using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SpeakingClub.Models
{
    public class BlogEditModel
    {
              // The unique identifier for the blog post.
        public int BlogId { get; set; }
        
        // Basic Blog properties.
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;

        // Embedded media properties.
        public string RawYT { get; set; } = string.Empty;
        public string RawMaps { get; set; } = string.Empty;

        // Category selection (assuming a blog may have one or many categories).
        public List<int> SelectedCategoryIds { get; set; } = new List<int>();

        // Quiz selection: if a blog associates with quizzes.
        // Use this if multiple quizzes are supported. Otherwise, you can use an int? for single quiz selection.
        public List<int>? SelectedQuizIds { get; set; }

        // The selected quiz question after a quiz is chosen.
        public int? SelectedQuestionId { get; set; }

        // Tags selection.
        public List<int> SelectedTagIds { get; set; } = new List<int>();

        // Toggle for displaying this blog on the home page.
        public bool IsHome { get; set; }

        // For updating the cover image.
        public IFormFile? ImageFile { get; set; }
        // NEW: This property is used to hold the cover image URL.
        public string CoverImageUrl { get; set; } = string.Empty;

        // Translation fields for various languages.
        public string TitleTR { get; set; } = string.Empty;
        public string ContentTR { get; set; } = string.Empty;
        public string TitleDE { get; set; } = string.Empty;
        public string ContentDE { get; set; } = string.Empty;
    }
}