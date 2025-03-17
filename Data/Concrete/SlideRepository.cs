using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SpeakingClub.Data.Abstract;
using SpeakingClub.Entity;

namespace SpeakingClub.Data.Concrete
{
    public class SlideRepository: GenericRepository<SlideShow>, ISlideRepository
    {
        public SlideRepository(SpeakingClubContext context) : base(context)
        {
        }
        
    }
}