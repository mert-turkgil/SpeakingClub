using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SpeakingClub.Entity;

namespace SpeakingClub.Data.Abstract
{
    public interface IQuizSubmissionRepository : IGenericRepository<QuizSubmission>
    {
        Task<IEnumerable<QuizSubmission>> GetSubmissionsByUserIdAsync(string userId);
        Task<double> GetAverageScoreByUserAsync(string userId);
        Task<IEnumerable<QuizSubmission>> GetSubmissionsByQuizIdAsync(int quizId);
    }
}