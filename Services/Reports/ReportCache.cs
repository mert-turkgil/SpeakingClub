using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;

namespace SpeakingClub.Services.Reports
{
    /// <summary>
    /// Short-lived cache for report figures (singleton). Every key carries a version number that grows whenever the
    /// data behind the reports changes (a new exam result, a quiz or question edited, an account deleted), so a
    /// teacher never sees stale numbers, while repeated views of the same filter cost nothing.
    /// </summary>
    public class ReportCache
    {
        private static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(10);
        private readonly IMemoryCache _cache;
        private int _version;

        public ReportCache(IMemoryCache cache) => _cache = cache;

        public int Version => Volatile.Read(ref _version);

        /// <summary>Called by ReportCacheInterceptor after saves that change exam results, quizzes or members.</summary>
        public void Invalidate() => Interlocked.Increment(ref _version);

        public async Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> create)
        {
            var fullKey = $"report:{Version}:{key}";
            if (_cache.TryGetValue(fullKey, out T? cached) && cached != null)
                return cached;
            var value = await create();
            _cache.Set(fullKey, value, new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = Lifetime, Size = 1 });
            return value;
        }
    }
}
