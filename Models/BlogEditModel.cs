using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace SpeakingClub.Models
{
   public class BlogEditModel
    {
        [Required]
        public int BlogId { get; set; }

        // Basic Information
        [Required(ErrorMessage = "Title is required")]
        [MaxLength(200, ErrorMessage = "Title cannot exceed 200 characters")]
        [Display(Name = "Blog Title")]
        public string Title { get; set; } = string.Empty;

        [MaxLength(250, ErrorMessage = "Description cannot exceed 250 characters")]
        [Display(Name = "Short Description")]
        public string? Description { get; set; }

        [Display(Name = "Content")]
        public string? Content { get; set; }

        // SEO Fields
        [MaxLength(200, ErrorMessage = "Slug cannot exceed 200 characters")]
        [Display(Name = "URL Slug")]
        [RegularExpression(@"^[a-z0-9\-]*$", ErrorMessage = "Slug can only contain lowercase letters, numbers, and hyphens")]
        public string? Slug { get; set; }

        [MaxLength(160, ErrorMessage = "Meta description should not exceed 160 characters")]
        [Display(Name = "Meta Description")]
        public string? MetaDescription { get; set; }

        [MaxLength(300, ErrorMessage = "Meta keywords cannot exceed 300 characters")]
        [Display(Name = "Meta Keywords")]
        public string? MetaKeywords { get; set; }

        [MaxLength(100, ErrorMessage = "Meta title cannot exceed 100 characters")]
        [Display(Name = "Meta Title (SEO Title)")]
        public string? MetaTitle { get; set; }

        [MaxLength(200, ErrorMessage = "Focus keyphrase cannot exceed 200 characters")]
        [Display(Name = "Focus Keyphrase")]
        public string? FocusKeyphrase { get; set; }

        [MaxLength(500, ErrorMessage = "Canonical URL cannot exceed 500 characters")]
        [Display(Name = "Canonical URL")]
        public string? CanonicalUrl { get; set; }

        [Display(Name = "No Index (Hide from search engines)")]
        public bool NoIndex { get; set; } = false;

        [Display(Name = "No Follow (Don't follow links)")]
        public bool NoFollow { get; set; } = false;

        // Open Graph / Social Media
        [MaxLength(200, ErrorMessage = "OG title cannot exceed 200 characters")]
        [Display(Name = "Social Media Title")]
        public string? OgTitle { get; set; }

        [MaxLength(300, ErrorMessage = "OG description cannot exceed 300 characters")]
        [Display(Name = "Social Media Description")]
        public string? OgDescription { get; set; }

        [Display(Name = "Social Media Image URL")]
        public string? OgImage { get; set; }

        // Traditional Fields
        [Display(Name = "Legacy URL")]
        public string? Url { get; set; }

        [Display(Name = "Update Cover Image")]
        public IFormFile? ImageFile { get; set; }

        public string? CoverImageUrl { get; set; }

        [Required(ErrorMessage = "Author name is required")]
        [MaxLength(100, ErrorMessage = "Author name cannot exceed 100 characters")]
        [Display(Name = "Author")]
        public string Author { get; set; } = string.Empty;

        [Display(Name = "Publish Date")]
        public DateTime Date { get; set; }

        [Display(Name = "YouTube Embed Code (Optional)")]
        public string? RawYT { get; set; }

        [Display(Name = "Google Maps Embed Code (Optional)")]
        public string? RawMaps { get; set; }

        [Display(Name = "Display on Home Page")]
        public bool IsHome { get; set; } = false;

        [Display(Name = "Published")]
        public bool IsPublished { get; set; } = true;

        [Display(Name = "Associated Quiz Question")]
        public int? SelectedQuestionId { get; set; }

        // Turkish Translation
        [MaxLength(200)]
        [Display(Name = "Turkish Title")]
        public string? TitleTR { get; set; }

        [MaxLength(250)]
        [Display(Name = "Turkish Description")]
        public string? DescriptionTR { get; set; }

        [Display(Name = "Turkish Content")]
        public string? ContentTR { get; set; }

        [MaxLength(200)]
        [Display(Name = "Turkish Slug")]
        public string? SlugTR { get; set; }

        [MaxLength(160)]
        [Display(Name = "Turkish Meta Description")]
        public string? MetaDescriptionTR { get; set; }

        [MaxLength(300)]
        [Display(Name = "Turkish Meta Keywords")]
        public string? MetaKeywordsTR { get; set; }

        [MaxLength(100)]
        [Display(Name = "Turkish Meta Title")]
        public string? MetaTitleTR { get; set; }

        [MaxLength(200)]
        [Display(Name = "Turkish Social Title")]
        public string? OgTitleTR { get; set; }

        [MaxLength(300)]
        [Display(Name = "Turkish Social Description")]
        public string? OgDescriptionTR { get; set; }

        // German Translation
        [MaxLength(200)]
        [Display(Name = "German Title")]
        public string? TitleDE { get; set; }

        [MaxLength(250)]
        [Display(Name = "German Description")]
        public string? DescriptionDE { get; set; }

        [Display(Name = "German Content")]
        public string? ContentDE { get; set; }

        [MaxLength(200)]
        [Display(Name = "German Slug")]
        public string? SlugDE { get; set; }

        [MaxLength(160)]
        [Display(Name = "German Meta Description")]
        public string? MetaDescriptionDE { get; set; }

        [MaxLength(300)]
        [Display(Name = "German Meta Keywords")]
        public string? MetaKeywordsDE { get; set; }

        [MaxLength(100)]
        [Display(Name = "German Meta Title")]
        public string? MetaTitleDE { get; set; }

        [MaxLength(200)]
        [Display(Name = "German Social Title")]
        public string? OgTitleDE { get; set; }

        [MaxLength(300)]
        [Display(Name = "German Social Description")]
        public string? OgDescriptionDE { get; set; }

        // Relationships
        public List<int>? SelectedCategoryIds { get; set; }
        public List<int>? SelectedTagIds { get; set; }
    }
}