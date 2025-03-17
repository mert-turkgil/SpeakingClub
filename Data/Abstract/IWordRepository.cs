using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SpeakingClub.Entity;

namespace SpeakingClub.Data.Abstract
{
    public interface IWordRepository: IGenericRepository<Word>
    {
        Task<Word?> GetWordByTermAsync(string term);
        Task<IEnumerable<Word>> SearchWordsByDefinitionKeywordAsync(string keyword);
    }
}