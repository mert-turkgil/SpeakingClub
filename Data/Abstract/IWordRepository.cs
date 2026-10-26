using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SpeakingClub.Entity;

namespace SpeakingClub.Data.Abstract
{
    public interface IWordRepository: IGenericRepository<Word>
    {
        new Task<Word?> GetByIdAsync(int id);
        /// <summary>Case-insensitive; "Hand" also finds "die Hand" and the other way round.</summary>
        Task<Word?> GetWordByTermAsync(string term);
        Task<IEnumerable<Word>> SearchWordsByDefinitionKeywordAsync(string keyword);
        /// <summary>Other entries that contain the search in their term (or, for longer searches, their definition).</summary>
        Task<List<Word>> GetRelatedAsync(string term, int? excludeId, int take);
        /// <summary>The most recently added terms, newest first.</summary>
        Task<List<string>> GetLatestTermsAsync(int take);
    }
}