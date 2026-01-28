using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SpeakingClub.Data;
using SpeakingClub.Data.Abstract;
using SpeakingClub.Entity;

namespace SpeakingClub.Data.Concrete
{
    public class BlogTranslationRepository: GenericRepository<BlogTranslation>, IBlogTranslationRepository
    {
        public BlogTranslationRepository(SpeakingClubContext context) : base(context)
        {
        }

        public async Task<BlogTranslation?> GetByBlogAndLanguageAsync(int blogId, string languageCode)
        {
            return await _dbSet.FirstOrDefaultAsync(bt => bt.BlogId == blogId && bt.LanguageCode == languageCode);
        }

        public async Task<IEnumerable<BlogTranslation>> GetTranslationsByBlogIdAsync(int blogId)
        {
            return await _dbSet
                .Where(bt => bt.BlogId == blogId)
                .ToListAsync();
        }

        public async Task<BlogTranslation?> GetBySlugAsync(string slug, string languageCode) => await _dbSet
                .FirstOrDefaultAsync(bt => bt.Slug == slug && bt.LanguageCode == languageCode);
    }
}