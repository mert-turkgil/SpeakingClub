using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SpeakingClub.Data.Abstract;
using SpeakingClub.Entity;

namespace SpeakingClub.Data.Concrete
{
    public class QuizRepository : GenericRepository<Quiz>, IQuizRepository
    {
        private readonly SpeakingClubContext _database;

        public QuizRepository(SpeakingClubContext context) : base(context)
        {
            _database = context;
        }
        public override async Task<IEnumerable<Quiz>> GetAllAsync()
        {
            return await _database.Quizzes
                .Include(q => q.Questions)
                .Include(q => q.Tags)
                .Include(q => q.Category)
                .Include(q => q.Words)
                .ToListAsync();
        }
        public async Task<IEnumerable<Quiz>> GetQuizzesByTeacherIdAsync(string teacherId)
        {
            return await _dbSet
                .Where(q => q.TeacherId == teacherId)
                .ToListAsync();
        }
        
        public async Task<QuizAnalysis> GetQuizAnalysisAsync(int quizId)
        {
            // This uses the context's QuizSubmissions DbSet to calculate analytics.
            var submissions = await _context.Set<QuizSubmission>()
                .Where(qs => qs.QuizId == quizId)
                .ToListAsync();
            
            int totalSubmissions = submissions.Count;
            double averageScore = totalSubmissions > 0 ? submissions.Average(qs => qs.Score) : 0;
            
            return new QuizAnalysis
            {
                QuizId = quizId,
                TotalSubmissions = totalSubmissions,
                AverageScore = averageScore
            };
        }
        
        public async Task<IEnumerable<Quiz>> SearchQuizzesByKeywordAsync(string keyword)
        {
            return await _dbSet
                .Where(q => q.Title!.Contains(keyword) || (q.Description != null && q.Description.Contains(keyword)))
                .ToListAsync();
        }
        public async Task<Quiz?> GetByIdAsync(int quizId)
        {
            return await _dbSet
                .Include(q => q.Questions)
                .ThenInclude(q => q.Answers)
                .Include(q=>q.Category).Where(q=>q.CategoryId==q.Category.CategoryId)
                .Include(q=>q.Blogs)
                .Include(q=>q.Tags)
                .Include(q=>q.Words)
                .FirstOrDefaultAsync(q => q.Id == quizId);
        }

    }
}