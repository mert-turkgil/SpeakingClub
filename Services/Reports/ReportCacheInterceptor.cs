using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SpeakingClub.Entity;
using SpeakingClub.Identity;

namespace SpeakingClub.Services.Reports
{
    /// <summary>
    /// Empties the report cache after every save that touched what the reports show: results, quizzes, questions,
    /// answer options, categories, tags (the level) or accounts (names). No controller action has to remember it.
    /// Registered on both database contexts in Program.cs (singleton, shared by all context instances).
    /// </summary>
    public class ReportCacheInterceptor : SaveChangesInterceptor
    {
        private readonly ReportCache _cache;
        // Contexts whose pending save is relevant; the cache is emptied only once the save went through,
        // so a report requested during the save can't store the old figures under the new version
        private readonly ConditionalWeakTable<DbContext, object> _pending = new();

        public ReportCacheInterceptor(ReportCache cache) => _cache = cache;

        public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
        {
            Remember(eventData.Context);
            return result;
        }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            Remember(eventData.Context);
            return ValueTask.FromResult(result);
        }

        public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
        {
            Release(eventData.Context, saved: true);
            return result;
        }

        public override ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData, int result, CancellationToken cancellationToken = default)
        {
            Release(eventData.Context, saved: true);
            return ValueTask.FromResult(result);
        }

        public override void SaveChangesFailed(DbContextErrorEventData eventData) => Release(eventData.Context, saved: false);

        public override Task SaveChangesFailedAsync(DbContextErrorEventData eventData, CancellationToken cancellationToken = default)
        {
            Release(eventData.Context, saved: false);
            return Task.CompletedTask;
        }

        private void Remember(DbContext? context)
        {
            if (context != null && context.ChangeTracker.Entries().Any(e => e.State != EntityState.Unchanged && Matters(e)))
                _pending.AddOrUpdate(context, true);
        }

        private void Release(DbContext? context, bool saved)
        {
            if (context != null && _pending.Remove(context) && saved)
                _cache.Invalidate();
        }

        private static bool Matters(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry) =>
            entry.Entity is QuizSubmission or QuizResponse or Quiz or Question or QuizAnswer or Category or Tag or User
            || entry.Metadata.HasSharedClrType;   // many-to-many links such as a quiz's tags
    }
}
