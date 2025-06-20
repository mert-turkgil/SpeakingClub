using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SpeakingClub.Data.Abstract;
using SpeakingClub.Entity;

namespace SpeakingClub.Data.Concrete
{
    public class QuestionRepository : GenericRepository<Question>, IQuestionRepository
    {
        private readonly new SpeakingClubContext _context;

        public QuestionRepository(SpeakingClubContext context) : base(context)
        {
            _context = context;
        }

        public async Task<Question?> GetByIdAsync(int questionId)
        {
            return await _context.Questions
                .Include(q => q.Answers)
                .FirstOrDefaultAsync(q => q.Id == questionId);
        }

        public async Task<IEnumerable<Question>> GetQuestionsByQuizIdAsync(int quizId)
        {
            return await _context.Questions
                .Where(q => q.QuizId == quizId)
                .Include(q => q.Answers)
                .ToListAsync();
        }

        public async Task AddAnswerAsync(int questionId, QuizAnswer answer)
        {
            var question = await _context.Questions
                .Include(q => q.Answers)
                .FirstOrDefaultAsync(q => q.Id == questionId);

            if (question != null)
            {
                question.Answers.Add(answer);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> HasCorrectAnswerAsync(int questionId)
        {
            return await _context.Questions
                .Where(q => q.Id == questionId)
                .SelectMany(q => q.Answers)
                .AnyAsync(a => a.IsCorrect);
        } 
        
        public override async Task<IEnumerable<Question>> GetAllAsync()
        {
            return await _context.Questions
                .Include(q => q.Answers)
                .Include(q => q.Quiz)
                .ToListAsync();
        }

    }
}