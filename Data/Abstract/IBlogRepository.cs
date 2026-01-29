using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SpeakingClub.Entity;

namespace SpeakingClub.Data.Abstract
{
    public interface IBlogRepository: IGenericRepository<Blog>
    {
        Task<IEnumerable<Blog>> GetRecentBlogsAsync(int count);
        Task<IEnumerable<Blog>> SearchBlogsByKeywordAsync(string keyword);
        Task<int> CountBlogsByDateAsync(DateTime date);
        Task<Blog?> GetByUrlAsync(string url);
        // Add this method to BlogRepository class
    }
}