using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using UI.Entity;
using Data.Abstract;
using UI.Data.Concrete;

namespace Data.Concrete.EfCore
{
    public class CarouselRepository : ICarouselRepository
    {
        private readonly ShopContext _context;

        public CarouselRepository(ShopContext context)
        {
            _context = context;
        }

        public async Task<Carousel?> GetByIdAsync(int id)
        {
            if (id <= 0) throw new ArgumentException("ID must be greater than 0.", nameof(id));

            return await _context.Carousels.FindAsync(id);
        }

        public async Task<List<Carousel>> GetAllAsync()
        {
            return await _context.Carousels.ToListAsync();
        }

        public async Task CreateAsync(Carousel carousel)
        {
            if (carousel == null) throw new ArgumentNullException(nameof(carousel));

            await _context.Carousels.AddAsync(carousel);
            await _context.SaveChangesAsync();
        }

        

        public async Task UpdateAsync(Carousel carousel)
        {
            if (carousel == null) throw new ArgumentNullException(nameof(carousel));

            _context.Carousels.Update(carousel);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            if (id <= 0) throw new ArgumentException("ID must be greater than 0.", nameof(id));

            var carousel = await GetByIdAsync(id);
            if (carousel != null)
            {
                _context.Carousels.Remove(carousel);
                await _context.SaveChangesAsync();
            }
            else
            {
                throw new InvalidOperationException($"Carousel with ID {id} does not exist.");
            }
        }

        public async Task<Carousel> CreateAndReturn(Carousel carousel)
        {
            if (carousel == null) throw new ArgumentNullException(nameof(carousel));

            await _context.Carousels.AddAsync(carousel);
            await _context.SaveChangesAsync();
            return carousel;
        }
    }
}
