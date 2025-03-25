using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using SpeakingClub.Services;

namespace SpeakingClub.Models
{
    public class BlogCreateModel
    {
        #nullable disable
        // Title and Main Content
        [Required(ErrorMessage = "Title is required.")]
        [MaxLength(200, ErrorMessage = "Title cannot exceed 200 characters.")]
        public string Title { get; set; }

        [Required(ErrorMessage = "Content is required.")]
        public string Content { get; set; } // Main content with image embedded

        // URL (Slug)
        [Required(ErrorMessage = "URL is required.")]
        [MaxLength(255, ErrorMessage = "URL cannot exceed 255 characters.")]
        public string Url { get; set; }

        // Image Upload
        public IFormFile ImageFile { get; set; }

        // PDF Upload
        public IFormFile PdfFile { get; set; }

        // Author
        [Required(ErrorMessage = "Author is required.")]
        [MaxLength(100, ErrorMessage = "Author cannot exceed 100 characters.")]
        public string Author { get; set; }

        // Embed Fields
        public string RawYT { get; set; } = string.Empty;
        public string RawMaps { get; set; } = string.Empty;

        [RequiredNonEmptyList(ErrorMessage = "At least one category must be selected.")]
        public List<int> SelectedCategoryIds { get; set; }
        //Products
        [RequiredNonEmptyList(ErrorMessage = "At least one product must be selected.")]
        public List<int> SelectedProductIds { get; set; } = new(); 

        // Translations
        [Required(ErrorMessage = "English title is required.")]
        public string TitleUS { get; set; }
        [Required(ErrorMessage = "English content is required.")]
        public string ContentUS { get; set; }

        [Required(ErrorMessage = "Turkish title is required.")]
        public string TitleTR { get; set; }
        [Required(ErrorMessage = "Turkish content is required.")]
        public string ContentTR { get; set; }

        [Required(ErrorMessage = "German title is required.")]
        public string TitleDE { get; set; }
        [Required(ErrorMessage = "German content is required.")]
        public string ContentDE { get; set; }

        // Helper Methods for Translations
        public string GetTitleByCulture(string culture)
        {
            return culture switch
            {
                "en-US" => TitleUS,
                "tr-TR" => TitleTR,
                "de-DE" => TitleDE,
                _ => Title
            };
        }

        public string GetContentByCulture(string culture, string imageUrl = "")
        {
            string content = culture switch
            {
                "en-US" => ContentUS,
                "tr-TR" => ContentTR,
                "de-DE" => ContentDE,
                _ => Content // Fallback to main content
            };

            if (!string.IsNullOrEmpty(imageUrl))
            {
                content += $"<br><img src='{imageUrl}' />";
            }

            return content;
}

    }
}