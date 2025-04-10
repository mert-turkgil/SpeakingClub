using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Storage;
using SpeakingClub.Entity;

namespace SpeakingClub.Data.Abstract
{
    public interface IUnitOfWork: IDisposable
    {
        IBlogRepository Blogs { get; }
        IQuizRepository  Quizzes { get; }
        IQuizAnswerRepository QuizAnswers { get; }
        IQuizSubmissionRepository QuizSubmissions { get; }
        IQuizResponseRepository QuizResponses { get; }
        ITagRepository Tags{get;}
        IWordRepository  Words { get; }
        ISlideRepository Slides { get; }
        IQuestionRepository Questions {get;}
        ICategoryRepository Categories { get; }
        IUserRepository Users { get; }
        IGenericRepository<T> GenericRepository<T>() where T : class;
        int Save();
        Task<int> SaveAsync();
        Task<IDbContextTransaction> BeginTransactionAsync();
    }
}