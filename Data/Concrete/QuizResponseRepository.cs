using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SpeakingClub.Data.Abstract;
using SpeakingClub.Entity;

namespace SpeakingClub.Data.Concrete
{
    public class QuizResponseRepository : GenericRepository<QuizResponse>, IQuizResponseRepository
    {
              public QuizResponseRepository(SpeakingClubContext context) : base(context)
        {
        }  
        public async Task<IEnumerable<QuizResponse>> GetResponsesForSubmissionAsync(int submissionId)
        {
            return await _dbSet
                .Where(qr => qr.QuizSubmissionId == submissionId)
                .ToListAsync();
        }
    }
}