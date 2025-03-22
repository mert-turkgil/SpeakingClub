using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SpeakingClub.Entity;

namespace SpeakingClub.Data.Abstract
{
    public interface IQuestionRepository : IGenericRepository<Question>
    {
        Task<Question?> GetByIdAsync(int questionId);
        Task<IEnumerable<Question>> GetQuestionsByQuizIdAsync(int quizId);
        Task AddAnswerAsync(int questionId, QuizAnswer answer);
        Task<bool> HasCorrectAnswerAsync(int questionId);
    }
}