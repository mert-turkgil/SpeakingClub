using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace SpeakingClub.Entity
{
    public class SlideShow
    {
        #nullable disable
        public SlideShow() { }

        [Key]
        public int SlideId { get; set; }

        public string CarouselTitle { get; set; } = string.Empty;
        public string CarouselImage { get; set; } = string.Empty;
        public string CarouselImage600w { get; set; } = string.Empty;
        public string CarouselImage1200w { get; set; } = string.Empty;
        public string CarouselDescription { get; set; } = string.Empty;
        public string CarouselLink { get; set; } = string.Empty;
        public string CarouselLinkText { get; set; } = string.Empty;

        public DateTime DateAdded { get; set; }
    }
}