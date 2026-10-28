using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SpeakingClub.Entity;

namespace SpeakingClub.Data.Abstract
{
    // Reports over many students go through Services/Reports (projections in SQL), never through this repository:
    // the old "load every submission with its answers" methods made the results page slower with every visitor.
    public interface IQuizSubmissionRepository : IGenericRepository<QuizSubmission>
    {
        Task<IEnumerable<QuizSubmission>> GetSubmissionsByUserIdAsync(string userId);

        Task<QuizSubmission?> GetLatestSubmissionByUserAndQuiz(string userId, int quizId);

        Task<QuizSubmission?> GetByIdWithResponsesAsync(int submissionId);
    }
}
