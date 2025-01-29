using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UI.Entity;

namespace UI.Data.Abstract
{
    public interface IBlogRepository
    {
        Task<List<Blog>> GetNewBlogsAsync(int count);
        Task<Blog> GetByIdAsync(int id);
        Task<List<Blog>> GetAllAsync();
        Task CreateAsync(Blog entity);
        Task UpdateAsync(Blog entity);
        Task DeleteAsync(int id);
    }
}