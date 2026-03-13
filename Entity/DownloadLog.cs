using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SpeakingClub.Entity
{
    public class DownloadLog
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(450)]
        public string UserId { get; set; } = string.Empty;

        [Required]
        [MaxLength(256)]
        public string UserEmail { get; set; } = string.Empty;

        [MaxLength(256)]
        public string UserFullName { get; set; } = string.Empty;

        [Required]
        public int BlogFileId { get; set; }

        [Required]
        [MaxLength(255)]
        public string FileName { get; set; } = string.Empty;

        public DateTime DownloadedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("BlogFileId")]
        public virtual BlogFile BlogFile { get; set; } = null!;
    }
}
