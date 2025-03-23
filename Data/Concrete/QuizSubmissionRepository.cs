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
        public QuizSubmissionRepository(SpeakingClubContext context) : base(context)
        {
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