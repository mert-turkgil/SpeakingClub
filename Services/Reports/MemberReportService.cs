using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SpeakingClub.Data;
using SpeakingClub.Identity;
using SpeakingClub.Models.Reports;
using SpeakingClub.Services.Email;

namespace SpeakingClub.Services.Reports
{
    /// <summary>
    /// Queries behind Admin → Kullanıcılar (the member report). The list, its filters, sorting and paging run in SQL:
    /// AspNetUsers joined with each member's exam activity (MemberActivity, QuizSubmissions grouped by user), so a
    /// page reads at most 100 accounts and never the password hashes. Charts get cached aggregates; one member's page
    /// reads only that member's exams and downloads. Dates are stored in UTC and shown in Turkish time.
    /// </summary>
    public class MemberReportService
    {
        private readonly ApplicationDbContext _identity;
        private readonly SpeakingClubContext _db;
        private readonly ExamReportService _exams;
        private readonly ReportCache _cache;

        public MemberReportService(ApplicationDbContext identity, SpeakingClubContext db, ExamReportService exams, ReportCache cache)
        {
            _identity = identity;
            _db = db;
            _exams = exams;
            _cache = cache;
        }

        private static readonly string[] Levels = { "A1", "A2", "B1", "B2", "C1", "C2" };

        public static string DisplayName(string? first, string? last, string? userName, string? email)
        {
            var name = string.Join(" ", new[] { first, last }.Where(s => !string.IsNullOrWhiteSpace(s))).Trim();
            return name.Length > 0 ? name : userName ?? email ?? "?";
        }

        /// <summary>Root accounts are never deleted; Admin accounts are edited and deleted only by Root; nobody deletes themselves here.</summary>
        public static MemberPermissions PermissionsFor(MemberRow member, string currentUserId, bool currentIsRoot)
        {
            var isSelf = member.Id == currentUserId;
            var isRoot = member.Roles.Contains("Root");
            var isAdmin = member.Roles.Contains("Admin");
            return new MemberPermissions(isSelf, !(isRoot || isAdmin) || currentIsRoot,
                !isSelf && !isRoot && (!isAdmin || currentIsRoot));
        }

        // ------------------------------------------------------------------
        // The filtered query
        // ------------------------------------------------------------------
        private sealed class Joined
        {
            public string Id { get; init; } = "";
            public string? FirstName { get; init; }
            public string? LastName { get; init; }
            public string? Email { get; init; }
            public string? UserName { get; init; }
            public DateTime? CreatedAt { get; init; }
            public DateTime? LastLoginAt { get; init; }
            public bool EmailConfirmed { get; init; }
            public DateTimeOffset? LockoutEnd { get; init; }
            public EmailTopics EmailOptOuts { get; init; }
            public int Attempts { get; init; }
            public int Quizzes { get; init; }
            public double? AverageScore { get; init; }
            public int? BestScore { get; init; }
            public DateTime? FirstExamAt { get; init; }
            public DateTime? LastExamAt { get; init; }
        }

        private async Task<IQueryable<Joined>> QueryAsync(MemberReportFilter filter)
        {
            var now = DateTime.UtcNow;
            var users = _identity.Users.AsNoTracking();
            var (from, to) = filter.Range();
            if (from is { } f) users = users.Where(u => u.CreatedAt >= f);
            if (to is { } t) users = users.Where(u => u.CreatedAt < t);
            if (filter.Q is { } q)
                users = users.Where(u => u.Email!.Contains(q) || u.UserName!.Contains(q) || u.FirstName.Contains(q) || u.LastName.Contains(q)
                                         || (u.FirstName + " " + u.LastName).Contains(q));

            if (filter.Role != null)
            {
                var roles = await _identity.Roles.AsNoTracking().Select(r => new { r.Id, r.Name }).ToListAsync();
                string[] Ids(params string[] names) => roles.Where(r => names.Contains(r.Name)).Select(r => r.Id).ToArray();
                var wanted = filter.Role switch { "teacher" => Ids("Teacher"), "admin" => Ids("Root", "Admin"), _ => Ids("Root", "Admin", "Teacher") };
                users = filter.Role == "student"
                    ? users.Where(u => !_identity.UserRoles.Any(ur => ur.UserId == u.Id && wanted.Contains(ur.RoleId)))
                    : users.Where(u => _identity.UserRoles.Any(ur => ur.UserId == u.Id && wanted.Contains(ur.RoleId)));
            }

            var nowOffset = DateTimeOffset.UtcNow;
            users = filter.Account switch
            {
                "unconfirmed" => users.Where(u => !u.EmailConfirmed),
                "locked" => users.Where(u => u.LockoutEnd != null && u.LockoutEnd > nowOffset),
                "google" => users.Where(u => _identity.UserLogins.Any(l => l.UserId == u.Id && l.LoginProvider == "Google")),
                _ => users
            };
            users = filter.Promo switch
            {
                "in" => users.Where(u => u.EmailOptOuts == EmailTopics.None),
                "out" => users.Where(u => u.EmailOptOuts != EmailTopics.None),
                _ => users
            };

            var joined =
                from u in users
                join m in _identity.MemberActivities on u.Id equals m.UserId into activity
                from m in activity.DefaultIfEmpty()
                select new Joined
                {
                    Id = u.Id, FirstName = u.FirstName, LastName = u.LastName, Email = u.Email, UserName = u.UserName,
                    CreatedAt = u.CreatedAt, LastLoginAt = u.LastLoginAt, EmailConfirmed = u.EmailConfirmed, LockoutEnd = u.LockoutEnd,
                    EmailOptOuts = u.EmailOptOuts,
                    Attempts = (int?)m.Attempts ?? 0, Quizzes = (int?)m.Quizzes ?? 0, AverageScore = m.AverageScore, BestScore = m.BestScore,
                    FirstExamAt = m.FirstExamAt, LastExamAt = m.LastExamAt
                };

            var activeSince = now.AddDays(-MemberReportFilter.ActiveDays);
            var dormantBefore = now.AddDays(-MemberReportFilter.DormantDays);
            return filter.Activity switch
            {
                "exams" => joined.Where(x => x.Attempts > 0),
                "none" => joined.Where(x => x.Attempts == 0),
                "active" => joined.Where(x => x.LastExamAt >= activeSince),
                "dormant" => joined.Where(x => x.Attempts > 0 && x.LastExamAt < dormantBefore),
                _ => joined
            };
        }

        private static IQueryable<Joined> Sort(IQueryable<Joined> query, string? sort) => sort switch
        {
            "oldest" => query.OrderBy(x => x.CreatedAt == null ? 1 : 0).ThenBy(x => x.CreatedAt).ThenBy(x => x.Id),
            "name" => query.OrderBy(x => x.FirstName).ThenBy(x => x.LastName).ThenBy(x => x.Email).ThenBy(x => x.Id),
            "activity" => query.OrderByDescending(x => x.LastLoginAt == null || x.LastExamAt > x.LastLoginAt ? x.LastExamAt : x.LastLoginAt).ThenBy(x => x.Id),
            "exams" => query.OrderByDescending(x => x.Attempts).ThenByDescending(x => x.LastExamAt).ThenBy(x => x.Id),
            "score" => query.OrderByDescending(x => x.AverageScore).ThenByDescending(x => x.Attempts).ThenBy(x => x.Id),
            _ => query.OrderByDescending(x => x.CreatedAt).ThenBy(x => x.Id)   // newest first; unknown dates last
        };

        // ------------------------------------------------------------------
        // List, headline figures, CSV
        // ------------------------------------------------------------------
        public async Task<(List<MemberRow> Rows, int Total)> PageAsync(MemberReportFilter filter)
        {
            var query = await QueryAsync(filter);
            var total = await query.CountAsync();
            var pages = Math.Max(1, (int)Math.Ceiling(total / (double)filter.Size));
            filter.Page = Math.Min(filter.Page, pages);
            return (await ToRowsAsync(await SliceAsync(query, filter.Sort, (filter.Page - 1) * filter.Size, filter.Size)), total);
        }

        /// <summary>
        /// One slice of the sorted list in two steps: the ids in order, then those rows in full. Sorting whole rows
        /// (the name columns are nvarchar(max)) made SQL Server ask for a memory grant of ~18 MB per page, and under
        /// memory pressure a query waits for its grant: the first visit took 20 seconds. Sorting ids needs almost none.
        /// </summary>
        private static async Task<List<Joined>> SliceAsync(IQueryable<Joined> query, string? sort, int skip, int take)
        {
            var ids = await Sort(query, sort).Select(x => x.Id).Skip(skip).Take(take).ToListAsync();
            if (ids.Count == 0) return new List<Joined>();
            var position = ids.Select((id, i) => (id, i)).ToDictionary(x => x.id, x => x.i);
            return (await query.Where(x => ids.Contains(x.Id)).ToListAsync()).OrderBy(x => position[x.Id]).ToList();
        }

        public Task<MemberKpis> KpisAsync(MemberReportFilter filter) => _cache.GetOrCreateAsync("members:kpis:" + filter.DataKey(), async () =>
        {
            var query = await QueryAsync(filter);
            var since = DateTime.UtcNow.AddDays(-30);
            var pass = ExamRules.PassScore;
            var kpis = await query.GroupBy(_ => 1).Select(g => new MemberKpis(
                g.Count(),
                g.Count(x => x.CreatedAt >= since),
                g.Count(x => x.EmailConfirmed),
                g.Count(x => x.Attempts > 0),
                g.Count(x => x.Attempts >= 2),
                g.Count(x => x.BestScore >= pass),
                g.Count(x => x.LastExamAt >= since),
                g.Count(x => x.EmailOptOuts == EmailTopics.None))).SingleOrDefaultAsync();   // grouped by a constant: one row or none
            return kpis ?? MemberKpis.Empty;
        });

        /// <summary>Every filtered member for the CSV export, in the list's order (read in batches of 500).</summary>
        public async IAsyncEnumerable<MemberRow> ExportAsync(MemberReportFilter filter)
        {
            var query = await QueryAsync(filter);
            for (var skip = 0; skip < 100_000; skip += 500)
            {
                var raw = await SliceAsync(query, filter.Sort, skip, 500);
                foreach (var row in await ToRowsAsync(raw)) yield return row;
                if (raw.Count < 500) yield break;
            }
        }

        private async Task<List<MemberRow>> ToRowsAsync(List<Joined> raw)
        {
            if (raw.Count == 0) return new List<MemberRow>();
            var ids = raw.Select(r => r.Id).ToList();
            var roles = (await (from ur in _identity.UserRoles
                                where ids.Contains(ur.UserId)
                                join r in _identity.Roles on ur.RoleId equals r.Id
                                select new { ur.UserId, r.Name }).ToListAsync())
                .ToLookup(x => x.UserId, x => x.Name ?? "");
            var google = (await _identity.UserLogins.Where(l => ids.Contains(l.UserId) && l.LoginProvider == "Google")
                .Select(l => l.UserId).ToListAsync()).ToHashSet();
            var now = DateTimeOffset.UtcNow;
            return raw.Select(r => new MemberRow(
                r.Id, DisplayName(r.FirstName, r.LastName, r.UserName, r.Email), r.Email, r.UserName,
                Local(r.CreatedAt), Local(r.LastLoginAt), r.EmailConfirmed, r.LockoutEnd > now, google.Contains(r.Id),
                roles[r.Id].OrderBy(x => x, StringComparer.Ordinal).ToList(), r.Attempts, r.Quizzes, r.AverageScore, r.BestScore,
                Local(r.LastExamAt), r.EmailOptOuts == EmailTopics.None)).ToList();
        }

        private static DateTime? Local(DateTime? utc) => utc is { } u ? ReportTime.ToLocal(u) : null;

        // ------------------------------------------------------------------
        // Charts (JSON for wwwroot/js/admin-reports.js)
        // ------------------------------------------------------------------

        /// <summary>Sign-ups and first exams per day/week/month, members over time, and the path from sign-up to a passed exam.</summary>
        public Task<object> GrowthAsync(MemberReportFilter filter) => _cache.GetOrCreateAsync("members:growth:" + filter.DataKey(), async () =>
        {
            var query = await QueryAsync(filter);
            var members = await query.Select(x => new { x.CreatedAt, x.FirstExamAt }).ToListAsync();
            var signups = members.Where(m => m.CreatedAt != null).Select(m => ReportTime.ToLocal(m.CreatedAt!.Value).Date).ToList();
            var firstExams = members.Where(m => m.FirstExamAt != null).Select(m => ReportTime.ToLocal(m.FirstExamAt!.Value).Date).ToList();

            var (from, to) = filter.Range();
            var start = from is { } f ? ReportTime.ToLocal(f).Date : signups.Concat(firstExams).DefaultIfEmpty(ReportTime.Today.ToDateTime(TimeOnly.MinValue)).Min();
            var end = to is { } t ? ReportTime.ToLocal(t).Date.AddDays(-1) : ReportTime.Today.ToDateTime(TimeOnly.MinValue);
            var days = (end - start).TotalDays;
            var unit = days <= 45 ? "day" : days <= 550 ? "week" : "month";
            DateTime Bucket(DateTime d) => unit switch
            {
                "day" => d,
                "week" => d.AddDays(-(((int)d.DayOfWeek + 6) % 7)),   // Monday
                _ => new DateTime(d.Year, d.Month, 1)
            };
            DateTime Next(DateTime d) => unit switch { "day" => d.AddDays(1), "week" => d.AddDays(7), _ => d.AddMonths(1) };

            var signupsBy = signups.GroupBy(Bucket).ToDictionary(g => g.Key, g => g.Count());
            var examsBy = firstExams.GroupBy(Bucket).ToDictionary(g => g.Key, g => g.Count());
            var points = new List<object>();
            var running = 0;
            for (var b = Bucket(start); b <= end && points.Count < 400; b = Next(b))
            {
                running += signupsBy.GetValueOrDefault(b);
                points.Add(new { date = b.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), signups = signupsBy.GetValueOrDefault(b), firstExams = examsBy.GetValueOrDefault(b), total = running });
            }

            var kpis = await KpisAsync(filter);
            return (object)new
            {
                n = members.Count,
                unknownDates = members.Count - signups.Count,
                unit,
                points,
                funnel = new[] { kpis.Members, kpis.Confirmed, kpis.Activated, kpis.TwoPlus, kpis.Passed }
            };
        });

        /// <summary>
        /// Cohorts: members grouped by sign-up month (the last 12), and the share of each group that took an exam
        /// 0, 1, 2… months later. Shows whether people keep coming back after the first weeks.
        /// </summary>
        public Task<object> CohortAsync(MemberReportFilter filter) => _cache.GetOrCreateAsync("members:cohort:" + filter.DataKey(), async () =>
        {
            var query = await QueryAsync(filter);
            var members = await query.Select(x => new { x.Id, x.CreatedAt }).ToListAsync();
            var today = ReportTime.Today;
            int MonthIndex(int year, int month) => year * 12 + month - 1;
            var current = MonthIndex(today.Year, today.Month);
            var first = current - 11;

            var cohortOf = new Dictionary<string, int>();
            foreach (var m in members)
            {
                if (m.CreatedAt is not { } created) continue;
                var local = ReportTime.ToLocal(created);
                var index = MonthIndex(local.Year, local.Month);
                if (index >= first) cohortOf[m.Id] = index;
            }

            // Months in which each member took an exam, in Turkish time (UTC+3 all year since 2016)
            var offset = ReportTime.Zone.BaseUtcOffset.TotalHours;
            var since = ReportTime.StartOfDayUtc(new DateOnly(first / 12, first % 12 + 1, 1));
            var examMonths = await _db.QuizSubmissions.AsNoTracking()
                .Where(s => s.SubmissionDate >= since)
                .Select(s => new { s.UserId, Local = s.SubmissionDate.AddHours(offset) })
                .GroupBy(x => new { x.UserId, x.Local.Year, x.Local.Month })
                .Select(g => new { g.Key.UserId, g.Key.Year, g.Key.Month })
                .ToListAsync();
            var active = examMonths.Where(x => cohortOf.ContainsKey(x.UserId))
                .Select(x => (x.UserId, Month: MonthIndex(x.Year, x.Month))).ToHashSet();

            var cohorts = cohortOf.GroupBy(kv => kv.Value).OrderBy(g => g.Key).Select(g =>
            {
                var ids = g.Select(kv => kv.Key).ToList();
                var values = Enumerable.Range(0, current - g.Key + 1)
                    .Select(k => (double?)ids.Count(id => active.Contains((id, g.Key + k))) / ids.Count).ToList();
                return new { month = $"{g.Key / 12:D4}-{g.Key % 12 + 1:D2}", size = ids.Count, values };
            }).ToList();
            return (object)new { cohorts, unknownDates = members.Count(m => m.CreatedAt == null) };
        });

        // ------------------------------------------------------------------
        // One member
        // ------------------------------------------------------------------
        public async Task<MemberDetail?> DetailAsync(string id)
        {
            var user = await _identity.Users.AsNoTracking().Where(u => u.Id == id).Select(u => new
            {
                u.Id, u.FirstName, u.LastName, u.Email, u.UserName, u.CreatedAt, u.LastLoginAt, u.EmailConfirmed, u.LockoutEnd,
                u.EmailOptOuts, u.PreferredCulture, u.PromoConsentAt, u.PromoConsentSource
            }).FirstOrDefaultAsync();
            if (user == null) return null;

            var roles = await (from ur in _identity.UserRoles
                               where ur.UserId == id
                               join r in _identity.Roles on ur.RoleId equals r.Id
                               select r.Name).ToListAsync();
            var google = await _identity.UserLogins.AnyAsync(l => l.UserId == id && l.LoginProvider == "Google");

            var quizzes = (await _exams.QuizOptionsAsync()).ToDictionary(q => q.Id);
            var exams = (await _db.QuizSubmissions.AsNoTracking()
                    .Where(s => s.UserId == id)
                    .OrderByDescending(s => s.SubmissionDate)
                    .Select(s => new { s.QuizSubmissionId, s.QuizId, s.SubmissionDate, s.AttemptNumber, s.Score, s.DurationSeconds, s.ViolationCount, s.EndReason })
                    .Take(500)
                    .ToListAsync())
                .Select(s => new MemberExam(s.QuizSubmissionId, s.QuizId,
                    quizzes.TryGetValue(s.QuizId, out var quiz) ? ExamReportService.QuizTitle(quiz) : $"#{s.QuizId}",
                    quiz?.Level, ReportTime.ToLocal(s.SubmissionDate), s.AttemptNumber, s.Score, s.DurationSeconds, s.ViolationCount,
                    s.EndReason ?? "submitted"))
                .ToList();

            var downloads = _db.DownloadLogs.AsNoTracking().Where(d => d.UserId == id);
            var recentDownloads = await downloads.OrderByDescending(d => d.DownloadedAt).Take(20)
                .Select(d => new { d.FileName, d.DownloadedAt }).ToListAsync();

            var now = DateTimeOffset.UtcNow;
            var member = new MemberRow(user.Id, DisplayName(user.FirstName, user.LastName, user.UserName, user.Email), user.Email, user.UserName,
                Local(user.CreatedAt), Local(user.LastLoginAt), user.EmailConfirmed, user.LockoutEnd > now, google,
                roles.Select(r => r ?? "").OrderBy(r => r, StringComparer.Ordinal).ToList(),
                exams.Count, exams.Select(e => e.QuizId).Distinct().Count(),
                exams.Count > 0 ? exams.Average(e => (double)e.Score) : null,
                exams.Count > 0 ? exams.Max(e => e.Score) : null,
                exams.Count > 0 ? exams[0].Local : null,
                user.EmailOptOuts == EmailTopics.None);

            return new MemberDetail
            {
                Member = member,
                PreferredCulture = user.PreferredCulture,
                EmailOptOuts = user.EmailOptOuts,
                PromoConsentLocal = Local(user.PromoConsentAt),
                PromoConsentSource = user.PromoConsentSource,
                LockoutEnd = user.LockoutEnd > now ? user.LockoutEnd : null,
                Exams = exams,
                Downloads = recentDownloads.Select(d => new MemberDownload(d.FileName, ReportTime.ToLocal(d.DownloadedAt))).ToList(),
                DownloadCount = await downloads.CountAsync(),
                HighestPassedLevel = exams.Where(e => e.Score >= ExamRules.PassScore && e.Level != null)
                    .Select(e => e.Level!).OrderByDescending(l => Array.IndexOf(Levels, l)).FirstOrDefault()
            };
        }
    }
}
