using System;
using System.Text;
using SpeakingClub.Entity;

namespace SpeakingClub.Services
{
    /// <summary>
    /// Helper class for generating SEO-related content like structured data
    /// </summary>
    public static class SeoHelper
    {
        /// <summary>
        /// Generates JSON-LD structured data for a blog article
        /// Uncomment and use this when blog is ready
        /// </summary>
        public static string GenerateBlogArticleSchema(Blog blog, string authorName = "Suna Türkgil")
        {
            // Use description if available, otherwise generate from content
            var description = !string.IsNullOrEmpty(blog.Description) 
                ? blog.Description 
                : StripHtmlTags(blog.Content, 160);
            
            var schema = $@"
    <script type=""application/ld+json"">
    {{
        ""@context"": ""https://schema.org"",
        ""@type"": ""BlogPosting"",
        ""headline"": ""{EscapeJson(blog.Title)}"",
        ""description"": ""{EscapeJson(description)}"",
        ""image"": ""https://almanca-konus.com{blog.Image}"",
        ""datePublished"": ""{blog.Date:yyyy-MM-ddTHH:mm:ssZ}"",
        ""dateModified"": ""{blog.Date:yyyy-MM-ddTHH:mm:ssZ}"",
        ""author"": {{
            ""@type"": ""Person"",
            ""name"": ""{authorName}""
        }},
        ""publisher"": {{
            ""@type"": ""Organization"",
            ""name"": ""SpeakingClub"",
            ""logo"": {{
                ""@type"": ""ImageObject"",
                ""url"": ""https://almanca-konus.com/img/header_logo.png""
            }}
        }},
        ""mainEntityOfPage"": {{
            ""@type"": ""WebPage"",
            ""@id"": ""https://almanca-konus.com/Home/BlogDetail/{blog.Url}""
        }}
    }}
    </script>";
            
            return schema;
        }

        /// <summary>
        /// Generates breadcrumb structured data
        /// </summary>
        public static string GenerateBreadcrumbSchema(params (string name, string url)[] breadcrumbs)
        {
            var items = new StringBuilder();
            for (int i = 0; i < breadcrumbs.Length; i++)
            {
                if (i > 0) items.Append(",");
                items.Append($@"
        {{
            ""@type"": ""ListItem"",
            ""position"": {i + 1},
            ""name"": ""{EscapeJson(breadcrumbs[i].name)}"",
            ""item"": ""https://almanca-konus.com{breadcrumbs[i].url}""
        }}");
            }

            var schema = $@"
    <script type=""application/ld+json"">
    {{
        ""@context"": ""https://schema.org"",
        ""@type"": ""BreadcrumbList"",
        ""itemListElement"": [{items}
        ]
    }}
    </script>";
            
            return schema;
        }

        /// <summary>
        /// Generates FAQ page structured data (useful for quiz pages)
        /// </summary>
        public static string GenerateFaqSchema(params (string question, string answer)[] faqs)
        {
            var items = new StringBuilder();
            for (int i = 0; i < faqs.Length; i++)
            {
                if (i > 0) items.Append(",");
                items.Append($@"
        {{
            ""@type"": ""Question"",
            ""name"": ""{EscapeJson(faqs[i].question)}"",
            ""acceptedAnswer"": {{
                ""@type"": ""Answer"",
                ""text"": ""{EscapeJson(faqs[i].answer)}""
            }}
        }}");
            }

            var schema = $@"
    <script type=""application/ld+json"">
    {{
        ""@context"": ""https://schema.org"",
        ""@type"": ""FAQPage"",
        ""mainEntity"": [{items}
        ]
    }}
    </script>";
            
            return schema;
        }

        /// <summary>
        /// Escapes special characters for JSON
        /// </summary>
        private static string EscapeJson(string text)
        {
            if (string.IsNullOrEmpty(text))
                return "";
            
            return text
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\n", "\\n")
                .Replace("\r", "\\r")
                .Replace("\t", "\\t");
        }

        /// <summary>
        /// Strips HTML tags from text (useful for meta descriptions)
        /// </summary>
        public static string StripHtmlTags(string html, int maxLength = 160)
        {
            if (string.IsNullOrEmpty(html))
                return "";

            // Remove HTML tags
            var text = System.Text.RegularExpressions.Regex.Replace(html, "<.*?>", " ");
            
            // Remove multiple spaces
            text = System.Text.RegularExpressions.Regex.Replace(text, @"\s+", " ").Trim();
            
            // Truncate to max length
            if (text.Length > maxLength)
            {
                text = text.Substring(0, maxLength - 3) + "...";
            }
            
            return text;
        }

        /// <summary>
        /// Generates a URL-friendly slug from text
        /// </summary>
        public static string GenerateSlug(string text)
        {
            if (string.IsNullOrEmpty(text))
                return "";

            // Convert to lowercase
            text = text.ToLowerInvariant();
            
            // Replace Turkish characters
            text = text.Replace("ı", "i")
                       .Replace("ğ", "g")
                       .Replace("ü", "u")
                       .Replace("ş", "s")
                       .Replace("ö", "o")
                       .Replace("ç", "c")
                       .Replace("İ", "i");
            
            // Remove invalid characters
            text = System.Text.RegularExpressions.Regex.Replace(text, @"[^a-z0-9\s-]", "");
            
            // Replace spaces with hyphens
            text = System.Text.RegularExpressions.Regex.Replace(text, @"\s+", "-");
            
            // Remove duplicate hyphens
            text = System.Text.RegularExpressions.Regex.Replace(text, @"-+", "-");
            
            return text.Trim('-');
        }
    }
}
