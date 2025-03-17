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
    }
}