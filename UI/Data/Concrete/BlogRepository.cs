using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using UI.Data.Abstract;
using UI.Entity;

namespace UI.Data.Concrete
{
    public class BlogRepository : IBlogRepository
    {
        private readonly ShopContext _context;

        public BlogRepository(ShopContext context)
        {
            _context = context;
        }

        public async Task<Blog> GetByIdAsync(int id)
        {
            #nullable disable
            return await _context.Blog.FindAsync(id);
        }

        public async Task<List<Blog>> GetAllAsync()
        {
            return await _context.Blog.ToListAsync();
        }

        public async Task CreateAsync(Blog entity)
        {
            await _context.Blog.AddAsync(entity);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Blog entity)
        {
            _context.Blog.Update(entity);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var blog = await GetByIdAsync(id);
            if (blog != null)
            {
                _context.Blog.Remove(blog);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<List<Blog>> GetNewBlogsAsync(int count)
        {
            return await _context.Blog
                .OrderByDescending(b => b.DateCreated)
                .Take(count)
                .ToListAsync();
        }

        
    }
}