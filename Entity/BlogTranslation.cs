using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SpeakingClub.Entity
{
    public class BlogTranslation
    {
        #nullable disable
        
        [Key]
        public int TranslationId { get; set; }

        [Required]
        [ForeignKey("Blog")]
        public int BlogId { get; set; }

        [Required]
        [MaxLength(10)]
        public string LanguageCode { get; set; } = string.Empty; // "tr", "de", "en", etc.

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(250)]
        public string Description { get; set; } = string.Empty;

        public string Content { get; set; } = string.Empty;

        // SEO fields per language
        [MaxLength(200)]
        public string Slug { get; set; } = string.Empty; // Language-specific URL slug
        
        [MaxLength(160)]
        public string MetaDescription { get; set; } = string.Empty;
        
        [MaxLength(300)]
        public string MetaKeywords { get; set; } = string.Empty;
        
        [MaxLength(100)]
        public string MetaTitle { get; set; } = string.Empty;

        // Social Media / OpenGraph per language
        [MaxLength(200)]
        public string OgTitle { get; set; } = string.Empty;
        
        [MaxLength(300)]
        public string OgDescription { get; set; } = string.Empty;

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime? LastModified { get; set; }

        // Navigation property
        public virtual Blog Blog { get; set; }
    }
}