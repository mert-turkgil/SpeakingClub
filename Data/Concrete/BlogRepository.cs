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
        public async Task<Blog?> GetByUrlAsync(string url)
        {
            // Include Category, Tags, Translations, and quizzes
            return await _dbSet
                .Include(b => b.Category)
                .Include(b => b.Tags)
                .Include(b => b.Translations)
                .Include(b => b.Quiz)
                    .ThenInclude(q => q.Questions)
                    .ThenInclude(q => q.Answers)
                .FirstOrDefaultAsync(b => 
                    b.Url.ToLower() == url.ToLower() || 
                    b.Slug.ToLower() == url.ToLower() ||
                    (b.Translations != null && b.Translations.Any(t => t.Slug != null && t.Slug.ToLower() == url.ToLower())));
        }
        public override async Task<IEnumerable<Blog>> GetAllAsync()
        {
            return await _dbSet
                .Include(b => b.Category)
                .Include(b => b.Tags)
                .Include(b => b.Translations)
                .Include(b => b.Quiz)
                .ToListAsync();
        }
        public override async Task<Blog?> GetAsync(int id)
        {
            return await _dbSet
                .Include(b => b.Category)
                .Include(b => b.Tags)
                .Include(b => b.Quiz)
                .Include(b => b.Translations)
                .FirstOrDefaultAsync(b => b.BlogId == id);
        }
    }
}