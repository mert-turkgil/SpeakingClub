using System.Collections.Generic;
using System.Threading.Tasks;
using UI.Entity;

namespace Data.Abstract
{
    public interface ICarouselRepository
    {
        Task<Carousel> CreateAndReturn(Carousel entity);
        Task<Carousel?> GetByIdAsync(int id);
        Task<List<Carousel>> GetAllAsync();
        Task CreateAsync(Carousel entity);
        Task UpdateAsync(Carousel entity);
        Task DeleteAsync(int id);
    }
}
