using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SpeakingClub.Data;
using SpeakingClub.Entity;
using SpeakingClub.Models.Reports;

namespace SpeakingClub.Services.Reports
{
    /// <summary>
    /// Queries behind Admin → Sınav sonuçları. Rules: only the rows and columns a page shows are read; filters run
    /// in SQL on the indexed columns (ReportIndexes migration); the analytics work on a slim cached list of the
    /// filtered attempts (about 50 bytes each), and only the question analysis reads answers, for one quiz.
    /// Read-only (AsNoTracking); dates are stored in UTC and shown in Turkish time (ReportTime).
    /// </summary>
    public class ExamReportService
    {
        private readonly SpeakingClubContext _db;
        private readonly ReportCache _cache;

        public ExamReportService(SpeakingClubContext db, ReportCache cache)
        {
            _db = db;
            _cache = cache;
        }

        private static bool IsGerman => CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "de";
        private static string T(string tr, string de) => IsGerman ? de : tr;
        private static string DeletedAccount => T("Silinmiş hesap", "Gelöschtes Konto");

        /// <summary>One filtered attempt, as the analytics need it.</summary>
        public record SlimRow(int Id, int QuizId, string UserId, DateTime Utc, int Score, int Attempt, int Duration, int Violations, string End);

        // ------------------------------------------------------------------
        // Options for the filter bar
        // ------------------------------------------------------------------
        public Task<List<ReportQuizOption>> QuizOptionsAsync() => _cache.GetOrCreateAsync("quizzes", async () =>
        {
            var quizzes = await _db.Quizzes.AsNoTracking()
                .Select(q => new
                {
                    q.Id, q.Title, q.TitleDe, q.CategoryId, q.TimeLimitMinutes,
                    Questions = q.Questions.Count,
                    Tags = q.Tags.Select(t => t.Name).ToList()
                })
                .ToListAsync();
            var levels = new[] { "A1", "A2", "B1", "B2", "C1", "C2" };
            return quizzes
                .Select(q => new ReportQuizOption(q.Id, q.Title ?? $"#{q.Id}", string.IsNullOrWhiteSpace(q.TitleDe) ? null : q.TitleDe,
                    q.Tags.Select(t => t?.Trim().ToUpperInvariant()).FirstOrDefault(t => t != null && levels.Contains(t)),
                    q.CategoryId, q.Questions, q.TimeLimitMinutes))
                .OrderBy(q => q.Title, StringComparer.CurrentCultureIgnoreCase)
                .ToList();
        });

        public Task<List<ReportCategoryOption>> CategoryOptionsAsync() => _cache.GetOrCreateAsync("categories", () =>
            _db.Categories.AsNoTracking().Where(c => c.Quizzes.Any())
                .OrderBy(c => c.Name).Select(c => new ReportCategoryOption(c.CategoryId, c.Name)).ToListAsync());

        public static string QuizTitle(ReportQuizOption quiz) => IsGerman && quiz.TitleDe != null ? quiz.TitleDe : quiz.Title;

        /// <summary>Attempts in progress right now (shown as a live badge).</summary>
        public Task<int> ActiveNowAsync()
        {
            var now = DateTime.UtcNow;
            return _db.QuizAttempts.CountAsync(a => a.SubmittedAt == null && a.ExpiresAt > now);
        }

        // ------------------------------------------------------------------
        // The filtered query
        // ------------------------------------------------------------------
        private async Task<IQueryable<QuizSubmission>> QueryAsync(ExamReportFilter filter, DateTime? from, DateTime? to)
        {
            var query = _db.QuizSubmissions.AsNoTracking();
            if (from is { } f) query = query.Where(s => s.SubmissionDate >= f);
            if (to is { } t) query = query.Where(s => s.SubmissionDate < t);

            if (filter.Quiz is { } quizId)
                query = query.Where(s => s.QuizId == quizId);
            if (filter.Level != null || filter.Category != null)
            {
                var ids = (await QuizOptionsAsync())
                    .Where(q => (filter.Level == null || q.Level == filter.Level) && (filter.Category == null || q.CategoryId == filter.Category))
                    .Select(q => q.Id).ToList();
                query = query.Where(s => ids.Contains(s.QuizId));
            }
            if (filter.Q is { } term)
            {
                query = query.Where(s => _db.ReportUsers.Any(u => u.UserId == s.UserId &&
                    (((u.FirstName ?? "") + " " + (u.LastName ?? "")).Contains(term) || (u.Email != null && u.Email.Contains(term)))));
            }
            if (filter.Status == "pass") query = query.Where(s => s.Score >= ExamRules.PassScore);
            if (filter.Status == "fail") query = query.Where(s => s.Score < ExamRules.PassScore);
            if (filter.End == "submitted") query = query.Where(s => s.EndReason == "submitted" || s.EndReason == null);
            else if (filter.End != null) query = query.Where(s => s.EndReason == filter.End);
            if (filter.First) query = query.Where(s => s.AttemptNumber <= 1);
            if (filter.Warn) query = query.Where(s => s.ViolationCount > 0);
            return query;
        }

        /// <summary>The filtered attempts in a slim form, cached; the analytics are computed from this list.</summary>
        public Task<List<SlimRow>> SlimAsync(ExamReportFilter filter, DateTime? from, DateTime? to) =>
            _cache.GetOrCreateAsync($"slim:{filter.DataKey()}:{from:O}:{to:O}", async () =>
                await (await QueryAsync(filter, from, to))
                    .Select(s => new SlimRow(s.QuizSubmissionId, s.QuizId, s.UserId, s.SubmissionDate, s.Score,
                        s.AttemptNumber, s.DurationSeconds, s.ViolationCount, s.EndReason ?? "submitted"))
                    .ToListAsync());

        public async Task<ExamKpis> KpisAsync(ExamReportFilter filter, DateTime? from, DateTime? to)
        {
            var rows = await SlimAsync(filter, from, to);
            if (rows.Count == 0) return ExamKpis.Empty;
            return new ExamKpis(rows.Count, rows.Select(r => r.UserId).Distinct().Count(),
                rows.Average(r => (double)r.Score),
                ItemStatistics.Median(rows.Select(r => (double)r.Score)),
                rows.Count(r => r.Score >= ExamRules.PassScore) / (double)rows.Count,
                ItemStatistics.Median(rows.Select(r => (double)r.Duration)),
                rows.Count(r => r.Violations > 0) / (double)rows.Count,
                rows.Count(r => r.End == "timeout") / (double)rows.Count);
        }

        // ------------------------------------------------------------------
        // Results table (one page)
        // ------------------------------------------------------------------
        public async Task<(List<ExamResultRow> Rows, int Total)> ResultsAsync(ExamReportFilter filter)
        {
            var (from, to) = filter.Range();
            var query = await QueryAsync(filter, from, to);
            var total = await query.CountAsync();
            var pages = Math.Max(1, (int)Math.Ceiling(total / (double)filter.Size));
            filter.Page = Math.Min(filter.Page, pages);
            var rows = await Project(Sort(Join(query), filter.Sort))
                .Skip((filter.Page - 1) * filter.Size)
                .Take(filter.Size)
                .ToListAsync();
            return (await ToRowsAsync(rows), total);
        }

        /// <summary>Every filtered result for the CSV export (newest first), in the same shape as the table.</summary>
        public async IAsyncEnumerable<ExamResultRow> ExportAsync(ExamReportFilter filter)
        {
            var (from, to) = filter.Range();
            var query = await QueryAsync(filter, from, to);
            // The order as ids first, then the rows 500 at a time: sorting every full row at once made SQL Server ask
            // for a large memory grant, and on a busy server the export timed out waiting for it
            var ids = await Sort(Join(query), filter.Sort).Select(x => x.S.QuizSubmissionId).Take(100_000).ToListAsync();
            foreach (var chunk in ids.Chunk(500))
            {
                var batch = await Project(Join(_db.QuizSubmissions.AsNoTracking().Where(s => chunk.Contains(s.QuizSubmissionId)))).ToListAsync();
                var position = chunk.Select((id, i) => (id, i)).ToDictionary(x => x.id, x => x.i);
                foreach (var result in await ToRowsAsync(batch.OrderBy(r => position[r.Id]).ToList())) yield return result;
            }
        }

        private sealed class Joined
        {
            public QuizSubmission S { get; init; } = null!;
            public ReportUser? U { get; init; }
        }

        private IQueryable<Joined> Join(IQueryable<QuizSubmission> query) =>
            from s in query
            join u in _db.ReportUsers on s.UserId equals u.UserId into users
            from u in users.DefaultIfEmpty()
            select new Joined { S = s, U = u };

        private static IQueryable<Joined> Sort(IQueryable<Joined> query, string? sort) => sort switch
        {
            "date_asc" => query.OrderBy(x => x.S.SubmissionDate),
            "score" => query.OrderByDescending(x => x.S.Score).ThenByDescending(x => x.S.SubmissionDate),
            "score_asc" => query.OrderBy(x => x.S.Score).ThenByDescending(x => x.S.SubmissionDate),
            "student" => query.OrderBy(x => x.U!.FirstName).ThenBy(x => x.U!.LastName).ThenByDescending(x => x.S.SubmissionDate),
            "quiz" => query.OrderBy(x => x.S.Quiz.Title).ThenByDescending(x => x.S.SubmissionDate),
            "duration" => query.OrderByDescending(x => x.S.DurationSeconds),
            "warnings" => query.OrderByDescending(x => x.S.ViolationCount).ThenByDescending(x => x.S.SubmissionDate),
            _ => query.OrderByDescending(x => x.S.SubmissionDate)
        };

        private sealed record RawRow(int Id, string UserId, string? First, string? Last, string? Email, int QuizId, DateTime Utc,
            int Attempt, int Score, int Correct, int Answered, int Total, int Duration, int Violations, string? End);

        private static IQueryable<RawRow> Project(IQueryable<Joined> query) =>
            query.Select(x => new RawRow(x.S.QuizSubmissionId, x.S.UserId, x.U!.FirstName, x.U!.LastName, x.U!.Email, x.S.QuizId,
                x.S.SubmissionDate, x.S.AttemptNumber, x.S.Score,
                x.S.QuizResponses.Count(r => r.IsCorrect == true),
                x.S.QuizResponses.Count(r => r.QuizAnswerId != null),
                x.S.QuizResponses.Count(),
                x.S.DurationSeconds, x.S.ViolationCount, x.S.EndReason));

        private async Task<List<ExamResultRow>> ToRowsAsync(IEnumerable<RawRow> rows)
        {
            var quizzes = (await QuizOptionsAsync()).ToDictionary(q => q.Id);
            return rows.Select(r =>
            {
                quizzes.TryGetValue(r.QuizId, out var quiz);
                return new ExamResultRow(r.Id, r.UserId, StudentName(r.First, r.Last, r.Email), r.Email, r.QuizId,
                    quiz != null ? QuizTitle(quiz) : $"#{r.QuizId}", quiz?.Level, ReportTime.ToLocal(r.Utc), Math.Max(1, r.Attempt),
                    r.Score, r.Correct, r.Answered, r.Total, r.Duration, quiz?.TimeLimitMinutes * 60, r.Violations, r.End ?? "submitted");
            }).ToList();
        }

        public static string StudentName(string? first, string? last, string? email)
        {
            var name = string.Join(' ', new[] { first, last }.Where(p => !string.IsNullOrWhiteSpace(p))).Trim();
            return name.Length > 0 ? name : email ?? DeletedAccount;
        }

        /// <summary>Names (or e-mail) of some students, for charts and tables built from the slim list.</summary>
        public async Task<Dictionary<string, string>> NamesAsync(IEnumerable<string> userIds)
        {
            var ids = userIds.Distinct().ToList();
            var users = await _db.ReportUsers.AsNoTracking().Where(u => ids.Contains(u.UserId)).ToListAsync();
            var names = users.ToDictionary(u => u.UserId, u => StudentName(u.FirstName, u.LastName, u.Email));
            foreach (var id in ids) names.TryAdd(id, DeletedAccount);
            return names;
        }

        // ------------------------------------------------------------------
        // Tab: overview (distribution, per quiz, trend, end reasons)
        // ------------------------------------------------------------------
        public async Task<object> OverviewAsync(ExamReportFilter filter)
        {
            var (from, to) = filter.Range();
            var rows = await SlimAsync(filter, from, to);
            var quizzes = (await QuizOptionsAsync()).ToDictionary(q => q.Id);

            var histogram = new int[10];
            foreach (var r in rows) histogram[Math.Min(9, r.Score / 10)]++;

            var perQuiz = rows.GroupBy(r => r.QuizId)
                .Select(g =>
                {
                    var box = ItemStatistics.Box(g.Select(r => (double)r.Score))!;
                    quizzes.TryGetValue(g.Key, out var quiz);
                    return new
                    {
                        quizId = g.Key,
                        title = quiz != null ? QuizTitle(quiz) : $"#{g.Key}",
                        level = quiz?.Level,
                        n = g.Count(),
                        students = g.Select(r => r.UserId).Distinct().Count(),
                        box.Min, box.Q1, box.Median, box.Q3, box.Max, box.Mean, box.Sd,
                        passRate = g.Count(r => r.Score >= ExamRules.PassScore) / (double)g.Count(),
                        medianDuration = ItemStatistics.Median(g.Select(r => (double)r.Duration))
                    };
                })
                .OrderByDescending(q => q.n)
                .ToList();

            // Trend by day for short periods, by week (Monday) otherwise
            var days = filter.Days();
            var byWeek = days == 0 || days > 62;
            var trend = rows
                .GroupBy(r =>
                {
                    var local = DateOnly.FromDateTime(ReportTime.ToLocal(r.Utc));
                    return byWeek ? local.AddDays(-(((int)local.DayOfWeek + 6) % 7)) : local;
                })
                .OrderBy(g => g.Key)
                .Select(g =>
                {
                    var box = ItemStatistics.Box(g.Select(r => (double)r.Score))!;
                    return new { date = g.Key.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), n = box.N, box.Mean, box.Median, box.Q1, box.Q3 };
                })
                .ToList();

            return new
            {
                n = rows.Count,
                pass = ExamRules.PassScore,
                histogram,
                quizzes = perQuiz,
                trend = new { unit = byWeek ? "week" : "day", points = trend },
                ends = new
                {
                    submitted = rows.Count(r => r.End == "submitted"),
                    timeout = rows.Count(r => r.End == "timeout"),
                    violations = rows.Count(r => r.End == "violations")
                }
            };
        }

        // ------------------------------------------------------------------
        // Tab: activity (weekday × hour, Turkish time)
        // ------------------------------------------------------------------
        public async Task<object> ActivityAsync(ExamReportFilter filter)
        {
            var (from, to) = filter.Range();
            var rows = await SlimAsync(filter, from, to);
            var cells = new int[7, 24];
            foreach (var r in rows)
            {
                var local = ReportTime.ToLocal(r.Utc);
                cells[((int)local.DayOfWeek + 6) % 7, local.Hour]++;
            }
            var list = new List<int[]>();
            for (var d = 0; d < 7; d++)
                for (var h = 0; h < 24; h++)
                    if (cells[d, h] > 0) list.Add(new[] { h, d, cells[d, h] });
            return new { n = rows.Count, cells = list, max = list.Count > 0 ? list.Max(c => c[2]) : 0 };
        }

        // ------------------------------------------------------------------
        // Tab: progress (first vs best attempt, score by attempt number)
        // ------------------------------------------------------------------
        public async Task<object> ProgressAsync(ExamReportFilter filter)
        {
            var (from, to) = filter.Range();
            var rows = await SlimAsync(filter.With(f => f.First = false), from, to);
            var quizzes = (await QuizOptionsAsync()).ToDictionary(q => q.Id);
            var pairs = rows.GroupBy(r => (r.UserId, r.QuizId))
                .Select(g =>
                {
                    var ordered = g.OrderBy(r => r.Utc).ToList();
                    var passedAt = ordered.FindIndex(r => r.Score >= ExamRules.PassScore);
                    return new
                    {
                        g.Key.UserId, g.Key.QuizId, attempts = ordered.Count,
                        first = ordered[0].Score, best = ordered.Max(r => r.Score), last = ordered[^1].Score,
                        passedAfter = passedAt < 0 ? (int?)null : passedAt + 1
                    };
                })
                .ToList();
            var retakes = pairs.Where(p => p.attempts > 1).ToList();
            var names = await NamesAsync(retakes.OrderByDescending(p => p.best - p.first).Take(30).Select(p => p.UserId));

            return new
            {
                students = pairs.Count,
                retakeShare = pairs.Count > 0 ? retakes.Count / (double)pairs.Count : (double?)null,
                meanGain = retakes.Count > 0 ? retakes.Average(p => (double)(p.best - p.first)) : (double?)null,
                byAttempt = rows.GroupBy(r => Math.Min(5, Math.Max(1, r.Attempt))).OrderBy(g => g.Key)
                    .Select(g => new { attempt = g.Key, n = g.Count(), mean = g.Average(r => (double)r.Score) }).ToList(),
                toPass = pairs.Where(p => p.passedAfter != null).GroupBy(p => Math.Min(5, p.passedAfter!.Value)).OrderBy(g => g.Key)
                    .Select(g => new { attempts = g.Key, n = g.Count() }).ToList(),
                neverPassed = pairs.Count(p => p.passedAfter == null),
                top = retakes.OrderByDescending(p => p.best - p.first).Take(30).Select(p => new
                {
                    name = names.GetValueOrDefault(p.UserId, DeletedAccount),
                    quiz = quizzes.TryGetValue(p.QuizId, out var quiz) ? QuizTitle(quiz) : $"#{p.QuizId}",
                    p.first, p.best, p.last, p.attempts
                }).ToList()
            };
        }

        // ------------------------------------------------------------------
        // Tab: integrity (warnings by type, per attempt)
        // ------------------------------------------------------------------
        private sealed record LogEvent(string Type, DateTime At, string? Id = null);

        private static List<LogEvent> ReadLog(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return new List<LogEvent>();
            try { return JsonSerializer.Deserialize<List<LogEvent>>(json) ?? new List<LogEvent>(); }
            catch (JsonException) { return new List<LogEvent>(); }
        }

        public async Task<object> IntegrityAsync(ExamReportFilter filter)
        {
            var (from, to) = filter.Range();
            var rows = await SlimAsync(filter, from, to);
            // Event logs of the most recent 3,000 attempts (each log is a short JSON array)
            var recent = rows.OrderByDescending(r => r.Utc).Take(3000).Select(r => r.Id).ToList();
            var logs = await _db.QuizAttempts.AsNoTracking()
                .Where(a => a.QuizSubmissionId != null && recent.Contains(a.QuizSubmissionId.Value) && a.IntegrityLog != null)
                .Select(a => a.IntegrityLog)
                .ToListAsync();
            var byType = logs.SelectMany(ReadLog).GroupBy(e => e.Type)
                .Select(g => new { type = g.Key, n = g.Count(), violation = ExamRules.ViolationTypes.Contains(g.Key) })
                .OrderByDescending(g => g.n).ToList();

            var flagged = rows.Where(r => r.Violations > 0).OrderByDescending(r => r.Violations).ThenByDescending(r => r.Utc).Take(50).ToList();
            var names = await NamesAsync(flagged.Select(r => r.UserId));
            var quizzes = (await QuizOptionsAsync()).ToDictionary(q => q.Id);
            var withWarnings = rows.Where(r => r.Violations > 0).ToList();
            return new
            {
                n = rows.Count,
                logged = logs.Count,
                withWarnings = withWarnings.Count,
                distribution = new[] { 0, 1, 2, 3 }.Select(v => rows.Count(r => v < 3 ? r.Violations == v : r.Violations >= 3)).ToArray(),
                meanScoreWith = withWarnings.Count > 0 ? withWarnings.Average(r => (double)r.Score) : (double?)null,
                meanScoreWithout = rows.Count > withWarnings.Count ? rows.Where(r => r.Violations == 0).Average(r => (double)r.Score) : (double?)null,
                ended = rows.Count(r => r.End == "violations"),
                byType,
                attempts = flagged.Select(r => new
                {
                    id = r.Id,
                    name = names.GetValueOrDefault(r.UserId, DeletedAccount),
                    quiz = quizzes.TryGetValue(r.QuizId, out var quiz) ? QuizTitle(quiz) : $"#{r.QuizId}",
                    warnings = r.Violations, r.Score, end = r.End,
                    date = ReportTime.ToLocal(r.Utc).ToString("yyyy-MM-ddTHH:mm", CultureInfo.InvariantCulture)
                }).ToList()
            };
        }

        // ------------------------------------------------------------------
        // Tabs: question analysis, student × question, effort (one quiz)
        // ------------------------------------------------------------------
        /// <param name="mode">first (each student's first attempt, default), best, or all attempts</param>
        public Task<(ItemAnalysis.Result Result, Dictionary<string, string> Names)?> ItemsAsync(ExamReportFilter filter, string? mode, bool excludeLowEffort)
        {
            mode = mode is "best" or "all" ? mode : "first";
            if (filter.Quiz is not { } quizId) return Task.FromResult<(ItemAnalysis.Result, Dictionary<string, string>)?>(null);
            return _cache.GetOrCreateAsync<(ItemAnalysis.Result, Dictionary<string, string>)?>($"items:{filter.DataKey()}:{mode}:{excludeLowEffort}", async () =>
            {
                var (from, to) = filter.Range();
                var attempts = await (await QueryAsync(filter.With(f => f.First = false), from, to))
                    .Select(s => new { s.QuizSubmissionId, s.UserId, s.Score, s.SubmissionDate, s.TimesRecorded })
                    .ToListAsync();
                var chosen = mode switch
                {
                    "all" => attempts,
                    "best" => attempts.GroupBy(a => a.UserId).Select(g => g.OrderByDescending(a => a.Score).ThenBy(a => a.SubmissionDate).First()).ToList(),
                    _ => attempts.GroupBy(a => a.UserId).Select(g => g.OrderBy(a => a.SubmissionDate).First()).ToList()
                };
                var ids = chosen.Select(a => a.QuizSubmissionId).ToList();

                var questions = await _db.Questions.AsNoTracking().Where(q => q.QuizId == quizId).OrderBy(q => q.Id)
                    .Select(q => new
                    {
                        q.Id, q.QuestionText,
                        Answers = q.Answers.OrderBy(a => a.Id).Select(a => new { a.Id, a.AnswerText, a.IsCorrect }).ToList()
                    })
                    .ToListAsync();
                var responses = await _db.QuizResponses.AsNoTracking()
                    .Where(r => ids.Contains(r.QuizSubmissionId))
                    .Select(r => new
                    {
                        r.QuizSubmissionId, QuestionId = r.QuestionId ?? (int?)r.QuizAnswer.QuestionId,
                        r.QuizAnswerId, r.IsCorrect, r.AnswerText, r.TimeTakenSeconds
                    })
                    .ToListAsync();

                var bySubmission = responses.GroupBy(r => r.QuizSubmissionId).ToDictionary(g => g.Key, g => g
                    .Where(r => r.QuestionId != null)
                    .GroupBy(r => r.QuestionId!.Value)
                    .ToDictionary(q => q.Key, q =>
                    {
                        var r = q.First();
                        var skipped = r.QuizAnswerId == null && (r.AnswerText == null || r.AnswerText == "No answer") && r.IsCorrect != true;
                        return new ItemAnalysis.ResponseInput(r.QuizAnswerId, skipped ? false : r.IsCorrect, skipped, r.TimeTakenSeconds);
                    }));

                var input = chosen.Select(a => new ItemAnalysis.AttemptInput(a.QuizSubmissionId, a.UserId, a.TimesRecorded,
                    bySubmission.TryGetValue(a.QuizSubmissionId, out var map) ? map : new Dictionary<int, ItemAnalysis.ResponseInput>())).ToList();
                var questionInput = questions.Select(q => new ItemAnalysis.QuestionInput(q.Id, Plain(q.QuestionText),
                    q.Answers.Select(a => new ItemAnalysis.OptionInput(a.Id, Plain(a.AnswerText), a.IsCorrect)).ToList())).ToList();

                var result = ItemAnalysis.Analyze(questionInput, input, excludeLowEffort);
                var names = await NamesAsync(result.Rows.Select(r => r.UserId));
                return (result, names);
            });
        }

        private static string Plain(string? html)
        {
            if (string.IsNullOrWhiteSpace(html)) return string.Empty;
            var text = System.Net.WebUtility.HtmlDecode(HtmlUtility.StripHtml(html)).Trim();
            return text.Length > 300 ? text[..297] + "…" : text;
        }

        // ------------------------------------------------------------------
        // One attempt in detail
        // ------------------------------------------------------------------
        public async Task<AttemptDetail?> AttemptAsync(int submissionId)
        {
            var s = await _db.QuizSubmissions.AsNoTracking()
                .Where(x => x.QuizSubmissionId == submissionId)
                .Select(x => new
                {
                    x.QuizSubmissionId, x.UserId, x.QuizId, x.SubmissionDate, x.StartedAt, x.AttemptNumber, x.Score, x.DurationSeconds,
                    x.ViolationCount, x.EndReason, x.AttemptId, x.TimesRecorded
                })
                .FirstOrDefaultAsync();
            if (s == null) return null;

            var quiz = (await QuizOptionsAsync()).FirstOrDefault(q => q.Id == s.QuizId);
            var user = await _db.ReportUsers.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == s.UserId);
            var responses = await _db.QuizResponses.AsNoTracking()
                .Where(r => r.QuizSubmissionId == submissionId)
                .OrderBy(r => r.QuizResponseId)   // the order the student saw the questions in
                .Select(r => new
                {
                    QuestionId = r.QuestionId ?? (int?)r.QuizAnswer.QuestionId, r.QuizAnswerId, Chosen = r.QuizAnswer.AnswerText,
                    r.AnswerText, r.IsCorrect, r.TimeTakenSeconds
                })
                .ToListAsync();
            var questionIds = responses.Where(r => r.QuestionId != null).Select(r => r.QuestionId!.Value).Distinct().ToList();
            var questions = await _db.Questions.AsNoTracking().Where(q => questionIds.Contains(q.Id))
                .Select(q => new { q.Id, q.QuestionText, Correct = q.Answers.Where(a => a.IsCorrect).Select(a => a.AnswerText).ToList() })
                .ToDictionaryAsync(q => q.Id);

            // How the whole class did on these questions: mean time (attempts with recorded times) and share correct
            var classStats = await _db.QuizResponses.AsNoTracking()
                .Where(r => r.QuestionId != null && questionIds.Contains(r.QuestionId.Value) && r.IsCorrect != null)
                .GroupBy(r => r.QuestionId!.Value)
                .Select(g => new
                {
                    g.Key,
                    P = g.Average(r => r.IsCorrect == true ? 1.0 : 0.0),
                    MeanTime = g.Where(r => r.QuizSubmission.TimesRecorded && r.QuizAnswerId != null).Average(r => (double?)r.TimeTakenSeconds)
                })
                .ToDictionaryAsync(g => g.Key);

            var answers = responses.Select((r, i) =>
            {
                questions.TryGetValue(r.QuestionId ?? 0, out var question);
                classStats.TryGetValue(r.QuestionId ?? 0, out var stats);
                var skipped = r.QuizAnswerId == null && (r.AnswerText == null || r.AnswerText == "No answer") && r.IsCorrect != true;
                var rapid = s.TimesRecorded && !skipped && stats?.MeanTime is { } mean && r.TimeTakenSeconds < ItemStatistics.RapidThreshold(mean);
                return new AttemptAnswer(i + 1, r.QuestionId,
                    question != null ? Plain(question.QuestionText) : T("(Bu soru silinmiş)", "(Diese Frage wurde gelöscht)"),
                    r.Chosen != null ? Plain(r.Chosen) : null,
                    question?.Correct.Select(Plain).ToList() ?? new List<string>(),
                    skipped ? false : r.IsCorrect, skipped, r.TimeTakenSeconds, stats?.MeanTime, rapid, stats?.P);
            }).ToList();

            var events = new List<AttemptEvent>();
            DateTime? started = s.StartedAt;
            if (s.AttemptId is { } attemptId)
            {
                var attempt = await _db.QuizAttempts.AsNoTracking().Where(a => a.Id == attemptId)
                    .Select(a => new { a.IntegrityLog, a.StartedAt }).FirstOrDefaultAsync();
                if (attempt != null)
                {
                    started ??= attempt.StartedAt;
                    events = ReadLog(attempt.IntegrityLog)
                        .OrderBy(e => e.At)
                        .Select(e => new AttemptEvent(e.Type, Math.Max(0, (int)(e.At - attempt.StartedAt).TotalSeconds), ExamRules.ViolationTypes.Contains(e.Type)))
                        .ToList();
                }
            }

            var timed = answers.Where(a => !a.Skipped && a.ClassMeanSeconds != null).ToList();
            double? rte = s.TimesRecorded && timed.Count > 0 ? timed.Count(a => !a.Rapid) / (double)timed.Count : null;

            return new AttemptDetail(s.QuizSubmissionId, s.UserId, StudentName(user?.FirstName, user?.LastName, user?.Email), user?.Email,
                s.QuizId, quiz != null ? QuizTitle(quiz) : $"#{s.QuizId}", quiz?.Level, ReportTime.ToLocal(s.SubmissionDate),
                started is { } st ? ReportTime.ToLocal(st) : null, Math.Max(1, s.AttemptNumber), s.Score, s.DurationSeconds,
                quiz?.TimeLimitMinutes * 60, s.ViolationCount, s.EndReason ?? "submitted", s.TimesRecorded, answers, events, rte);
        }
    }
}
