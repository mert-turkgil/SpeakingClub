using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SpeakingClub.Entity;

namespace SpeakingClub.Data.Abstract
{
    public interface ICategoryRepository : IGenericRepository<Category>
    {
        Task<Category?> GetByIdAsync(int id);        
    }
}