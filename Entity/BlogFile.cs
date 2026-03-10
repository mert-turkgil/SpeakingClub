using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SpeakingClub.Entity
{
    public class BlogFile
    {
        #nullable disable

        [Key]
        public int FileId { get; set; }

        [Required]
        [ForeignKey("Blog")]
        public int BlogId { get; set; }

        [Required]
        [MaxLength(255)]
        public string OriginalFileName { get; set; } = string.Empty;

        [Required]
        [MaxLength(500)]
        public string StoredFilePath { get; set; } = string.Empty;

        [Required]
        [MaxLength(10)]
        public string FileExtension { get; set; } = string.Empty;

        [MaxLength(100)]
        public string ContentType { get; set; } = string.Empty;

        public long FileSizeBytes { get; set; }

        [MaxLength(500)]
        public string DisplayName { get; set; } = string.Empty;

        public int DownloadCount { get; set; } = 0;

        public int SortOrder { get; set; } = 0;

        public DateTime UploadedDate { get; set; } = DateTime.UtcNow;

        // Navigation property
        public virtual Blog Blog { get; set; }
    }
}
