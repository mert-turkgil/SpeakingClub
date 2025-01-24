using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using UI.Data.Abstract;
using UI.Data.Concrete;
using UI.Entity;

namespace UI.Data.Conrete
{
    #nullable disable
    public class QuizRepository : IQuizRepository
    {
        private readonly ShopContext _context;

        public QuizRepository(ShopContext context)
        {
            _context = context;
        }

        public async Task<Quiz> GetByIdAsync(int id)
        {
            return await _context.Quiz.FindAsync(id);
        }

        public async Task<List<Quiz>> GetAllAsync()
        {
            return await _context.Quiz.ToListAsync();
        }

        public async Task CreateAsync(Quiz entity)
        {
            await _context.Quiz.AddAsync(entity);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Quiz entity)
        {
            _context.Quiz.Update(entity);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var quiz = await GetByIdAsync(id);
            if (quiz != null)
            {
                _context.Quiz.Remove(quiz);
                await _context.SaveChangesAsync();
            }
        }
    }
}