using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace SpeakingClub.Models
{
    public class CarouselViewModel
    {
        #nullable disable
        public int CarouselId { get; set; }

        // General Information
        [Required, StringLength(100)]
        public string CarouselTitle { get; set; }

        [StringLength(500)]
        public string CarouselDescription { get; set; }

        [StringLength(255)]
        public string CarouselLink { get; set; }

        [StringLength(100)]
        public string CarouselLinkText { get; set; }

        public DateTime DateAdded { get; set; } = DateTime.UtcNow;

        // Images
        public IFormFile CarouselImage { get; set; }
        public IFormFile CarouselImage600w { get; set; }
        public IFormFile CarouselImage1200w { get; set; }

        // Translations
        public CarouselTranslation TranslationsTR { get; set; } = new();
        public CarouselTranslation TranslationsDE { get; set; } = new();
    }

    public class CarouselTranslation
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string LinkText { get; set; }
    }
}