using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SpeakingClub.Data.Abstract
{
    public interface IGenericRepository<T> where T : class
    {
        Task<IEnumerable<T>> GetAllAsync();
        Task<T> GetAsync(int id);
        Task AddAsync(T entity);
        void Update(T entity);
        void Remove(T entity);
    }
}