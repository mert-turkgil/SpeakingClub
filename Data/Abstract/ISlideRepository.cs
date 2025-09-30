using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SpeakingClub.Entity;

namespace SpeakingClub.Data.Abstract
{
    public interface ISlideRepository : IGenericRepository<SlideShow>
    {
        Task<SlideShow> CreateAndReturn(SlideShow entity);
        Task<SlideShow?> GetByIdAsync(int id);
        new Task<List<SlideShow>> GetAllAsync();
        Task CreateAsync(SlideShow entity);
        Task UpdateAsync(SlideShow entity);
        Task DeleteAsync(int id);
    }
}