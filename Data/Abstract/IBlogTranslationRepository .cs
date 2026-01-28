using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SpeakingClub.Entity;

namespace SpeakingClub.Data.Abstract
{
    public interface IBlogTranslationRepository : IGenericRepository<BlogTranslation>
    {
        Task<BlogTranslation?> GetByBlogAndLanguageAsync(int blogId, string languageCode);
        Task<IEnumerable<BlogTranslation>> GetTranslationsByBlogIdAsync(int blogId);
        Task<BlogTranslation?> GetBySlugAsync(string slug, string languageCode);
    }
}