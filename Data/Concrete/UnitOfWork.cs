using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Storage;
using SpeakingClub.Data.Abstract;
using SpeakingClub.Entity;
using SpeakingClub.Identity;

namespace SpeakingClub.Data.Concrete
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _identityContext;
        private readonly SpeakingClubContext _context;
        private readonly Dictionary<Type, object> _repositories = new Dictionary<Type, object>();
        private IBlogRepository? _blogs;
        private IQuizRepository? _quizzes;
        private IQuizAnswerRepository? _quizAnswers;
        private IQuizSubmissionRepository? _quizSubmissions;
        private IQuizResponseRepository? _quizResponses;
        private IWordRepository? _words;
        private ICategoryRepository? _category;
        private IQuestionRepository ? _questions;
        private ISlideRepository? _slideRepository;
        private IUserRepository? _users;
        private ITagRepository? _tags;

        public UnitOfWork(SpeakingClubContext context, ApplicationDbContext identityContext)
        {
            _context = context;
            _identityContext = identityContext;
        }
        public IUserRepository Users => _users ??= new UserRepository(_identityContext);
        public ITagRepository Tags=>_tags ??=new TagRepository(_context);
        public IQuestionRepository Questions => _questions ??= new QuestionRepository(_context);
        public IBlogRepository Blogs => _blogs ??= new BlogRepository(_context);
        public IQuizRepository Quizzes => _quizzes ??= new QuizRepository(_context);
        public IQuizAnswerRepository QuizAnswers => _quizAnswers ??= new QuizAnswerRepository(_context);
        public IQuizSubmissionRepository QuizSubmissions => _quizSubmissions ??= new QuizSubmissionRepository(_context);
        public IQuizResponseRepository QuizResponses => _quizResponses ??= new QuizResponseRepository(_context);
        public IWordRepository Words => _words ??= new WordRepository(_context);
        public ISlideRepository Slides => _slideRepository ??= new SlideRepository(_context);
        public ICategoryRepository Categories =>_category ??= new CategoryRepository(_context);
        public IGenericRepository<T> GenericRepository<T>() where T : class
        {
            if (!_repositories.ContainsKey(typeof(T)))
            {
                var repositoryInstance = new GenericRepository<T>(_context);
                _repositories.Add(typeof(T), repositoryInstance);
            }
            return (IGenericRepository<T>)_repositories[typeof(T)];
        }
        public int Save()
        {
            return _context.SaveChanges();
        }

        public async Task<int> SaveAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public void Dispose()
        {
            _context.Dispose();
        }
        public async Task<IDbContextTransaction> BeginTransactionAsync()
        {
            return await _context.Database.BeginTransactionAsync();
        }
    }
}