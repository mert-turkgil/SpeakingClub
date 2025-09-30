using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SpeakingClub.Data.Abstract;
using SpeakingClub.Entity;

namespace SpeakingClub.Data.Concrete
{
    public class SlideRepository: GenericRepository<SlideShow>, ISlideRepository
    {
        private readonly new SpeakingClubContext _context;
        public SlideRepository(SpeakingClubContext context) : base(context)
        {
            _context = context;
        }

        public async Task<SlideShow?> GetByIdAsync(int id)
        {
            if (id <= 0) throw new ArgumentException("ID must be greater than 0.", nameof(id));

            return await _context.Slide.FindAsync(id);
        }

        public new async Task<List<SlideShow>> GetAllAsync()
        {
            return await _context.Slide.ToListAsync();
        }

        public async Task CreateAsync(SlideShow slideShow)
        {
            if (slideShow == null) throw new ArgumentNullException(nameof(slideShow));

            await _context.Slide.AddAsync(slideShow);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(SlideShow carousel)
        {
            if (carousel == null) throw new ArgumentNullException(nameof(carousel));

            _context.Slide.Update(carousel);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            if (id <= 0) throw new ArgumentException("ID must be greater than 0.", nameof(id));

            var carousel = await GetByIdAsync(id);
            if (carousel != null)
            {
                _context.Slide.Remove(carousel);
                await _context.SaveChangesAsync();
            }
            else
            {
                throw new InvalidOperationException($"SlideShow with ID {id} does not exist.");
            }
        }

        public async Task<SlideShow> CreateAndReturn(SlideShow carousel)
        {
            if (carousel == null) throw new ArgumentNullException(nameof(carousel));

            await _context.Slide.AddAsync(carousel);
            await _context.SaveChangesAsync();
            return carousel;
        }
    }
}