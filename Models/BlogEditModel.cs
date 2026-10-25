using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SpeakingClub.Models
{
   public class BlogEditModel : BlogFormModel
    {
        [Required]
        public int BlogId { get; set; }

        public string? CoverImageUrl { get; set; }

        // Existing files for display in edit form
        public List<BlogFileViewModel>? ExistingFiles { get; set; }
    }

    public class BlogFileViewModel
    {
        public int FileId { get; set; }
        public string OriginalFileName { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string FileExtension { get; set; } = string.Empty;
        public long FileSizeBytes { get; set; }
        public int DownloadCount { get; set; }
        public DateTime UploadedDate { get; set; }
    }
}