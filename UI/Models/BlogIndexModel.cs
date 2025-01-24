using System;
using System.Collections.Generic;
using UI.Entity;

namespace UI.Models
{
    public class BlogIndexModel
    {
        #nullable disable
        // List of Blogs
        public List<Blog> Blogs { get; set; }

        // Translations for Title
        public string Title { get; set; }
        public string TitleUS { get; set; }
        public string TitleTR { get; set; }
        public string TitleDE { get; set; }
        public string TitleFR { get; set; }

        // Translations for Content
        public string Content { get; set; }
        public string ContentUS { get; set; }
        public string ContentTR { get; set; }
        public string ContentDE { get; set; }
        public string ContentFR { get; set; }

        /// <summary>
        /// Get Title based on culture.
        /// </summary>
        /// <param name="culture">Culture code (e.g., "en-US").</param>
        /// <returns>Localized title.</returns>
        public string GetTitleByCulture(string culture)
        {
            return culture switch
            {
                "en-US" => !string.IsNullOrEmpty(TitleUS) ? TitleUS : Title,
                "tr-TR" => !string.IsNullOrEmpty(TitleTR) ? TitleTR : Title,
                "de-DE" => !string.IsNullOrEmpty(TitleDE) ? TitleDE : Title,
                "fr-FR" => !string.IsNullOrEmpty(TitleFR) ? TitleFR : Title,
                _ => Title // Fallback to default Title
            };
        }

        /// <summary>
        /// Get Content based on culture and optionally include an image.
        /// </summary>
        /// <param name="culture">Culture code (e.g., "en-US").</param>
        /// <param name="imageUrl">Optional image URL.</param>
        /// <returns>Localized content with optional image.</returns>
        public string GetContentByCulture(string culture, string imageUrl = "")
        {
            string content = culture switch
            {
                "en-US" => !string.IsNullOrEmpty(ContentUS) ? ContentUS : Content,
                "tr-TR" => !string.IsNullOrEmpty(ContentTR) ? ContentTR : Content,
                "de-DE" => !string.IsNullOrEmpty(ContentDE) ? ContentDE : Content,
                "fr-FR" => !string.IsNullOrEmpty(ContentFR) ? ContentFR : Content,
                _ => Content // Fallback to default Content
            };

            // Append image if provided
            if (!string.IsNullOrEmpty(imageUrl))
            {
                content += $"<br><img src='{imageUrl}' />";
            }

            return content;
        }

        /// <summary>
        /// Paginate the blogs into groups of three.
        /// </summary>
        /// <param name="pageIndex">Page number starting at 0.</param>
        /// <param name="pageSize">Number of blogs per page (default: 3).</param>
        /// <returns>List of blogs for the requested page.</returns>
        public List<Blog> GetBlogsByPage(int pageIndex, int pageSize = 3)
        {
            return Blogs
                .Skip(pageIndex * pageSize) // Skip previous pages
                .Take(pageSize)            // Take next 'pageSize' blogs
                .ToList();
        }
    }
}
