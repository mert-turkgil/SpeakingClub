using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UI.Data.Conrete;
using UI.Entity;

namespace UI.Data.Abstract
{
    
    public interface IQuizRepository
    {
        #nullable disable
        Task<Quiz> GetByIdAsync(int id);
        Task<List<Quiz>> GetAllAsync();
        Task CreateAsync(Quiz entity);
        Task UpdateAsync(Quiz entity);
        Task DeleteAsync(int id);
    }
}