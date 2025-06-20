using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SpeakingClub.Data.Abstract;
using SpeakingClub.Entity;

namespace SpeakingClub.Data.Concrete
{
    public class CategoryRepository : GenericRepository<Category>, ICategoryRepository
    {
         private readonly SpeakingClubContext _database;
        public CategoryRepository(SpeakingClubContext context) : base(context)
        {
            _database = context;
        }
        public async Task<Category?> GetByIdAsync(int id)
        {
            return await _database.Categories
                .Include(c => c.Blogs)
                .Include(c => c.Quizzes)
                .FirstOrDefaultAsync(c => c.CategoryId == id);
        }

    }
}