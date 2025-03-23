using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SpeakingClub.Data.Abstract;
using SpeakingClub.Entity;

namespace SpeakingClub.Data.Concrete
{
    public class WordRepository : GenericRepository<Word>, IWordRepository
    {
        private readonly SpeakingClubContext _appContext;

        public WordRepository(SpeakingClubContext context) : base(context)
        {
            _appContext = context;
        }
        
        public async Task<Word?> GetWordByTermAsync(string term)
        {
            return await _appContext.Words
                .FirstOrDefaultAsync(w => w.Term.ToLower() == term.ToLower());
        }
        
        public async Task<IEnumerable<Word>> SearchWordsByDefinitionKeywordAsync(string keyword)
        {
            return await _dbSet
                .Where(w => w.Term.Contains(keyword) || w.Definition.Contains(keyword))
                .ToListAsync();
        }
    }
}