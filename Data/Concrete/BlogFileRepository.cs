using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SpeakingClub.Data.Abstract;
using SpeakingClub.Entity;

namespace SpeakingClub.Data.Concrete
{
    public class BlogFileRepository : GenericRepository<BlogFile>, IBlogFileRepository
    {
        public BlogFileRepository(SpeakingClubContext context) : base(context)
        {
        }

        public async Task<IEnumerable<BlogFile>> GetFilesByBlogIdAsync(int blogId)
        {
            return await _dbSet
                .Where(f => f.BlogId == blogId)
                .OrderBy(f => f.SortOrder)
                .ToListAsync();
        }
    }
}
