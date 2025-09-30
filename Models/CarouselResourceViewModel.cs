using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SpeakingClub.Models
{
    public class CarouselResourceViewModel
    {
        public List<CarouselItemViewModel> CarouselItems { get; set; } = new List<CarouselItemViewModel>();
    }
    public class CarouselItemViewModel
    {
        #nullable disable
        public int CarouselId { get; set; }
        public string CarouselImage { get; set; }
        public string CarouselImage600w { get; set; }
        public string CarouselImage1200w { get; set; }
        public string CarouselTitle { get; set; }
        public string CarouselDescription { get; set; }
        public string CarouselLink { get; set; }
        public string CarouselLinkText { get; set; }
    }
}