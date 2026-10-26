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
        private static readonly string[] Articles = { "der", "die", "das" };
        private readonly SpeakingClubContext _appContext;

        public WordRepository(SpeakingClubContext context) : base(context)
        {
            _appContext = context;
        }

        /// <summary>"  die   Hand " → "die Hand"</summary>
        public static string NormalizeTerm(string? term) =>
            string.Join(' ', (term ?? "").Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

        /// <summary>"die Hand" → "Hand"; words without a German article stay as they are.</summary>
        public static string WithoutArticle(string term)
        {
            var space = term.IndexOf(' ');
            return space > 0 && Articles.Contains(term[..space], StringComparer.OrdinalIgnoreCase) ? term[(space + 1)..] : term;
        }

        // Case-insensitive, and the article does not matter: "Hand", "die Hand" and
        // "DIE hand" all find an entry saved as "die Hand" (and the other way round).
        public async Task<Word?> GetWordByTermAsync(string term)
        {
            var query = NormalizeTerm(term);
            if (query.Length == 0) return null;
            var bare = WithoutArticle(query);
            var variants = new[] { query, bare }.Concat(Articles.Select(a => a + " " + bare))
                .Select(v => v.ToLowerInvariant())
                .Distinct()
                .ToList();

            var matches = await _appContext.Words
                .Where(w => variants.Contains(w.Term.ToLower()))
                .ToListAsync();
            // The exact spelling wins over an article variant
            return matches
                .OrderBy(w => string.Equals(NormalizeTerm(w.Term), query, StringComparison.OrdinalIgnoreCase) ? 0 : 1)
                .ThenBy(w => w.WordId)
                .FirstOrDefault();
        }

        public override async Task<Word?> GetByIdAsync(int id)
        {
            return await _appContext.Words
                .Include(w => w.Quizzes)
                .FirstOrDefaultAsync(w => w.WordId == id);
        }

        public async Task<IEnumerable<Word>> SearchWordsByDefinitionKeywordAsync(string keyword)
        {
            return await _appContext.Words
                .Where(w => w.Term.Contains(keyword) || w.Definition.Contains(keyword))
                .ToListAsync();
        }

        // Entries whose term contains the search ("Übergabe" → "die Patientenübergabe"), then,
        // for longer searches, entries that mention it in their definition.
        public async Task<List<Word>> GetRelatedAsync(string term, int? excludeId, int take)
        {
            var bare = WithoutArticle(NormalizeTerm(term));
            if (bare.Length < 2) return new List<Word>();

            var related = await _appContext.Words.AsNoTracking()
                .Where(w => w.WordId != excludeId && w.Term.Contains(bare))
                .OrderBy(w => w.Term.Length)
                .Take(take)
                .ToListAsync();

            // Definitions hold HTML, so very short searches would match tag names
            if (related.Count < take && bare.Length >= 4)
            {
                var known = related.Select(w => w.WordId).ToList();
                related.AddRange(await _appContext.Words.AsNoTracking()
                    .Where(w => w.WordId != excludeId && !known.Contains(w.WordId) && w.Definition.Contains(bare))
                    .OrderBy(w => w.Term)
                    .Take(take - related.Count)
                    .ToListAsync());
            }
            return related;
        }

        public async Task<List<string>> GetLatestTermsAsync(int take)
        {
            return await _appContext.Words.AsNoTracking()
                .OrderByDescending(w => w.WordId)
                .Select(w => w.Term)
                .Take(take)
                .ToListAsync();
        }
    }
}
