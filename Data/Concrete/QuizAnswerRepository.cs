using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SpeakingClub.Data.Abstract;
using SpeakingClub.Entity;

namespace SpeakingClub.Data.Concrete
{
    public class QuizAnswerRepository: GenericRepository<QuizAnswer>, IQuizAnswerRepository
    {
        public QuizAnswerRepository(SpeakingClubContext context) : base(context)
        {
        }
        public async Task<IEnumerable<QuizAnswer>> GetCorrectAnswersForQuizAsync(int quizId)
        {
            return await _dbSet
                .Include(a => a.Question) // Ensure the Question is loaded (if not using lazy loading)
                .Where(a => a.Question.QuizId == quizId && a.IsCorrect)
                .ToListAsync();
        }
    }
}