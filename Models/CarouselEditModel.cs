using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace SpeakingClub.Models
{
    public class CarouselEditModel
    {
        #nullable disable

        [Required]
        public int CarouselId { get; set; }

        [StringLength(100)]
        public string CarouselTitle { get; set; }

        [StringLength(500)]
        public string CarouselDescription { get; set; }

        [StringLength(255)]
        public string CarouselLink { get; set; }

        [StringLength(100)]
        public string CarouselLinkText { get; set; }

        [Required]
        public DateTime DateAdded { get; set; }

        // Image URL for existing image
        [StringLength(255)]
        public string CarouselImageUrl { get; set; }


        // Main Image
        public string CarouselImagePath { get; set; } // Path for displaying the image
        public IFormFile CarouselImage { get; set; }  // Uploading new image

        // 600w Image
        public string CarouselImage600wPath { get; set; } // Path for displaying 600w image
        public IFormFile CarouselImage600w { get; set; }  // Uploading new 600w image

        // 1200w Image
        public string CarouselImage1200wPath { get; set; } // Path for displaying 1200w image
        public IFormFile CarouselImage1200w { get; set; }  // Uploading new 1200w image


        // Translations
        public string CarouselTitleTR { get; set; }
        public string CarouselDescriptionTR { get; set; }
        public string CarouselLinkTextTR { get; set; }

        public string CarouselTitleDE { get; set; }
        public string CarouselDescriptionDE { get; set; }
        public string CarouselLinkTextDE { get; set; }
    }
}