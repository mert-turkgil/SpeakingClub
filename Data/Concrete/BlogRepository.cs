using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SpeakingClub.Data.Abstract;
using SpeakingClub.Entity;

namespace SpeakingClub.Data.Concrete
{
    public class BlogRepository: GenericRepository<Blog>, IBlogRepository
    {
        public BlogRepository(SpeakingClubContext context) : base(context)
        {
        }
        
        public async Task<IEnumerable<Blog>> GetRecentBlogsAsync(int count)
        {
            return await _dbSet
                .OrderByDescending(b => b.Date)
                .Take(count)
                .ToListAsync();
        }
        
        public async Task<IEnumerable<Blog>> SearchBlogsByKeywordAsync(string keyword)
        {
            return await _dbSet
                .Where(b => b.Title.Contains(keyword) || b.Content.Contains(keyword))
                .ToListAsync();
        }
        
        public async Task<int> CountBlogsByDateAsync(DateTime date)
        {
            return await _dbSet.CountAsync(b => b.Date.Date == date.Date);
        }
    }
}