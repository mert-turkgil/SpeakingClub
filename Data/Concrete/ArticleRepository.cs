using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SpeakingClub.Data.Abstract;
using SpeakingClub.Entity;

namespace SpeakingClub.Data.Concrete
{
    public class ArticleRepository : GenericRepository<Article>, IArticleRepository
    {
        public ArticleRepository(SpeakingClubContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Article>> GetArticlesByTeacherIdAsync(string teacherId)
        {
            return await _dbSet
                .Where(a => a.TeacherId == teacherId)
                .ToListAsync();
        }
        
        public async Task<IEnumerable<Article>> SearchArticlesByKeywordAsync(string keyword)
        {
            return await _dbSet
                .Where(a => a.Title.Contains(keyword) || a.Content.Contains(keyword))
                .ToListAsync();
        }
        
        public async Task<int> CountArticlesByTeacherAsync(string teacherId)
        {
            return await _dbSet.CountAsync(a => a.TeacherId == teacherId);
        }
    }
}