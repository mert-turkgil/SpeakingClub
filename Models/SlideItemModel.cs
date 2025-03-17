using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SpeakingClub.Models
{
    public class SlideItemModel
    {
        public List<SlideModel> SlideItems { get; set; } = new List<SlideModel>();
    }
    public class SlideModel
    {
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