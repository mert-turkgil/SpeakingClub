using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SpeakingClub.Entity;

namespace SpeakingClub.Data.Abstract
{
    public interface IQuizAnswerRepository: IGenericRepository<QuizAnswer>
    {
        Task<IEnumerable<QuizAnswer>> GetCorrectAnswersForQuizAsync(int quizId);
    }
}