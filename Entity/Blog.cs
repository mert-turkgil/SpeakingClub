using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using SpeakingClub.Models;

namespace SpeakingClub.Entity
{
    public class Blog
    {
        #nullable disable
        public Blog()
        {
            Quiz = new HashSet<Quiz>();
            Tags = new List<Tag>();
            Translations = new List<BlogTranslation>();
        }

        [Key]
        public int BlogId { get; set; }

        // Default language content (English or your primary language)
        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(250)]
        public string Description { get; set; } = string.Empty;

        public string Content { get; set; } = string.Empty;

        // SEO Fields
        [MaxLength(200)]
        public string Slug { get; set; } = string.Empty; // URL-friendly version: "my-blog-post"
        
        [MaxLength(160)]
        public string MetaDescription { get; set; } = string.Empty; // For search engine snippets
        
        [MaxLength(300)]
        public string MetaKeywords { get; set; } = string.Empty; // SEO keywords, comma-separated
        
        [MaxLength(100)]
        public string MetaTitle { get; set; } = string.Empty; // Override title for SEO (optional)
        
        [MaxLength(500)]
        public string CanonicalUrl { get; set; } = string.Empty; // Prevent duplicate content issues
        
        [MaxLength(200)]
        public string FocusKeyphrase { get; set; } = string.Empty; // Primary SEO keyword/phrase
        
        public bool NoIndex { get; set; } = false; // Prevent search engine indexing if true
        public bool NoFollow { get; set; } = false; // Prevent following links if true

        // Social Media / OpenGraph
        [MaxLength(200)]
        public string OgTitle { get; set; } = string.Empty; // Open Graph title
        
        [MaxLength(300)]
        public string OgDescription { get; set; } = string.Empty; // Open Graph description
        
        [MaxLength(500)]
        public string OgImage { get; set; } = string.Empty; // Open Graph image URL

        // Existing fields
        public string Url { get; set; } = string.Empty;
        public string Image { get; set; } = string.Empty;
        public DateTime Date { get; set; }

        [MaxLength(100)]
        public string Author { get; set; } = string.Empty;

        // Optional embedded content
        public string RawYT { get; set; } = string.Empty;
        public string RawMaps { get; set; } = string.Empty;

        public bool isHome { get; set; }
        public bool IsPublished { get; set; } = true; // Control visibility
        public int ViewCount { get; set; } = 0; // Track popularity for SEO
        public DateTime? LastModified { get; set; } // For sitemap and search engines

        // Relationships
        public virtual ICollection<Quiz> Quiz { get; set; }
        
        public int? CategoryId { get; set; }
        public Category Category { get; set; }
        
        public ICollection<Tag> Tags { get; set; }
        
        public int? SelectedQuestionId { get; set; }

        // NEW: Multilingual support
        public virtual ICollection<BlogTranslation> Translations { get; set; }
    }
}