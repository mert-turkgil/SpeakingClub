using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SpeakingClub.Data.Abstract;
using SpeakingClub.Entity;

namespace SpeakingClub.Data.Concrete
{
    public class CategoryRepository : GenericRepository<Category>, ICategoryRepository
    {
        public CategoryRepository(SpeakingClubContext context) : base(context)
        {
        }
    }
}