using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using UI.Entity;

namespace UI.Entity
{
    public class Carousel
    {
        #nullable disable
        public Carousel() { } // Parameterless constructor

        [Key]
        public int CarouselId { get; set; }

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