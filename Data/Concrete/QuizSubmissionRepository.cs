using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SpeakingClub.Data.Abstract;
using SpeakingClub.Entity;

namespace SpeakingClub.Data.Concrete
{
    public class QuizSubmissionRepository : GenericRepository<QuizSubmission>, IQuizSubmissionRepository
    {
        protected new readonly SpeakingClubContext _context;
        protected new readonly DbSet<QuizSubmission> _dbSet;
        public QuizSubmissionRepository(SpeakingClubContext context) : base(context)
        {
            _context = context;
            _dbSet = context.Set<QuizSubmission>();
        }
        
        public async Task<IEnumerable<QuizSubmission>> GetSubmissionsByUserIdAsync(string userId)
        {
            return await _dbSet
                .Where(qs => qs.UserId == userId)
                .ToListAsync();
        }
        
        public async Task<double> GetAverageScoreByUserAsync(string userId)
        {
            var submissions = await _dbSet.Where(qs => qs.UserId == userId).ToListAsync();
            return submissions.Any() ? submissions.Average(qs => qs.Score) : 0;
        }
        
        public async Task<IEnumerable<QuizSubmission>> GetSubmissionsByQuizIdAsync(int quizId)
        {
            return await _dbSet.Where(qs => qs.QuizId == quizId).ToListAsync();
        }
        public async Task<IEnumerable<QuizSubmission>> GetAllAtOnceAsync()
        {
            return await _dbSet
                .Include(s => s.QuizResponses)
                    .ThenInclude(r => r.QuizAnswer)
                        .ThenInclude(a => a.Question)
                .Include(s => s.Quiz)
                .ToListAsync();
        }
        public async Task<List<QuizSubmission>> GetAllWithIncludesAsync()
        {
            return await _context.QuizSubmissions
                .Include(qs => qs.Quiz).ThenInclude(q => q.Questions).ThenInclude(q => q.Answers)
                .Include(qs => qs.QuizResponses).ThenInclude(qr => qr.QuizAnswer).ThenInclude(qa => qa.Question)
                .OrderByDescending(qs => qs.SubmissionDate)
                .ToListAsync();
        }

        public async Task<QuizSubmission?> GetLatestSubmissionByUserAndQuiz(string userId, int quizId)
        {
            return await _dbSet
                .Include(s => s.QuizResponses)
                    .ThenInclude(r => r.QuizAnswer)
                        .ThenInclude(a => a.Question)
                .Include(s => s.Quiz)
                .Where(s => s.UserId == userId && s.QuizId == quizId)
                .OrderByDescending(s => s.SubmissionDate)
                .FirstOrDefaultAsync();
        }
    }
}