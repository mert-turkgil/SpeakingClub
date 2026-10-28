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
        
        // One student's results (index UserId, QuizId, SubmissionDate)
        public async Task<IEnumerable<QuizSubmission>> GetSubmissionsByUserIdAsync(string userId)
        {
            return await _dbSet
                .Where(qs => qs.UserId == userId)
                .ToListAsync();
        }

        public async Task<QuizSubmission?> GetByIdWithResponsesAsync(int submissionId)
        {
            return await _dbSet
                .Include(s => s.QuizResponses)
                    .ThenInclude(r => r.QuizAnswer)
                .Include(s => s.Quiz)
                .FirstOrDefaultAsync(s => s.QuizSubmissionId == submissionId);
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