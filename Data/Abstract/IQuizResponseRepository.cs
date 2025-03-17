using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SpeakingClub.Entity;

namespace SpeakingClub.Data.Abstract
{
    public interface IQuizResponseRepository: IGenericRepository<QuizResponse>
    {
        Task<IEnumerable<QuizResponse>> GetResponsesForSubmissionAsync(int submissionId);
    }
}