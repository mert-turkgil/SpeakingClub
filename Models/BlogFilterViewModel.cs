using System.Collections.Generic;

namespace SpeakingClub.Models
{
    #nullable disable
    public class BlogFilterViewModel
    {
        // Filters
        public string Category { get; set; }
        public string Tag { get; set; }         // New: filter by tag
        public List<string> AvailableTags { get; set; } = new List<string>();
        public string SearchTerm { get; set; }

        // Paging
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        // Total number of matching blogs (for display)
        public int TotalBlogs { get; set; }

        // Data
        public List<SpeakingClub.Entity.Blog> Blogs { get; set; } = new List<SpeakingClub.Entity.Blog>();
        public List<string> Categories { get; set; } = new List<string>();

        // UI Text Labels
        public string BlogList_Title { get; set; }
        public string BlogList_Description { get; set; }
        public string BlogList_SearchLabel { get; set; }
        public string BlogList_AllCategories { get; set; }
        public string BlogList_TagLabel { get; set; }           // New: label for tag filter
        public string BlogList_TagPlaceholder { get; set; }     // New: placeholder for tag filter
        public string BlogList_ApplyFiltersButton { get; set; }
        public string BlogList_NoPostsMessage { get; set; }
        public string BlogList_ReadMore { get; set; }
    }
}
