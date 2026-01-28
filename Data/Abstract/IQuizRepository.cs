using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SpeakingClub.Entity;

namespace SpeakingClub.Data.Abstract
{
    public interface IQuizRepository: IGenericRepository<Quiz>
    {
        Task<IEnumerable<Quiz>> GetQuizzesByTeacherNameAsync(string teacherName);
        Task<QuizAnalysis> GetQuizAnalysisAsync(int quizId);
        Task<IEnumerable<Quiz>> SearchQuizzesByKeywordAsync(string keyword);
        new Task<Quiz?> GetByIdAsync(int quizId);
        Task<Quiz?> GetByIdWithQuestions(int quizId);
        void Update(Quiz quiz, bool modifyTeacherName = true);
    }
}