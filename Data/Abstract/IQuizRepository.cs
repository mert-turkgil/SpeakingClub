using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SpeakingClub.Entity;

namespace SpeakingClub.Data.Abstract
{
    public interface IQuizRepository: IGenericRepository<Quiz>
    {
        Task<IEnumerable<Quiz>> GetQuizzesByTeacherIdAsync(string teacherId);
        Task<QuizAnalysis> GetQuizAnalysisAsync(int quizId);
        Task<IEnumerable<Quiz>> SearchQuizzesByKeywordAsync(string keyword);
    }
}