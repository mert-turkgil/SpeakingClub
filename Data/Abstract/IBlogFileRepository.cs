using System.Collections.Generic;
using System.Threading.Tasks;
using SpeakingClub.Entity;

namespace SpeakingClub.Data.Abstract
{
    public interface IBlogFileRepository : IGenericRepository<BlogFile>
    {
        Task<IEnumerable<BlogFile>> GetFilesByBlogIdAsync(int blogId);
    }
}
