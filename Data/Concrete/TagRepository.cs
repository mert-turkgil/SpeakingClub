using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SpeakingClub.Data.Abstract;
using SpeakingClub.Entity;

namespace SpeakingClub.Data.Concrete
{
    public class TagRepository : GenericRepository<Tag>, ITagRepository
    {
        private readonly SpeakingClubContext _database;

        public TagRepository(SpeakingClubContext context) : base(context)
        {
            _database = context;
        }

        public override async Task<IEnumerable<Tag>> GetAllAsync()
        {
            return await _database.Tags
                .Include(t => t.Blogs)
                .Include(t => t.Quizzes)
                .ToListAsync();
        }
    }
}