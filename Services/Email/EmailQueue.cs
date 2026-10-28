using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SpeakingClub.Data;
using SpeakingClub.Entity;
using SpeakingClub.Identity;

namespace SpeakingClub.Services.Email
{
    /// <summary>Wakes the outbox worker when something was queued, so a sign-up or result e-mail leaves at once.</summary>
    public class EmailSignal
    {
        private readonly SemaphoreSlim _signal = new(0, 1);

        public void Wake()
        {
            try { if (_signal.CurrentCount == 0) _signal.Release(); }
            catch (SemaphoreFullException) { /* already awake */ }
        }

        public Task<bool> WaitAsync(TimeSpan timeout, CancellationToken ct) => _signal.WaitAsync(timeout, ct);
    }

    /// <summary>
    /// Puts e-mails in the outbox (EmailOutbox). Nothing is sent here: EmailOutboxService sends them at the allowed
    /// pace and logs the outcome, so a slow or broken mail server never slows down or breaks a page. Every e-mail
    /// has a dedupe key, so queueing the same thing twice (a retried request) sends it once.
    /// </summary>
    public class EmailQueue
    {
        private readonly SpeakingClubContext _db;
        private readonly ApplicationDbContext _identity;
        private readonly EmailSignal _signal;
        private readonly EmailOptions _options;
        private readonly ILogger<EmailQueue> _logger;

        public EmailQueue(SpeakingClubContext db, ApplicationDbContext identity, EmailSignal signal, IOptions<EmailOptions> options, ILogger<EmailQueue> logger)
        {
            _db = db;
            _identity = identity;
            _signal = signal;
            _options = options.Value;
            _logger = logger;
        }

        public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

        public static string Link(string path) => SeoHelper.SiteUrl + path;

        /// <summary>A link that opens in the recipient's language (QueryStringRequestCultureProvider).</summary>
        public static string Link(string path, string culture) =>
            SeoHelper.SiteUrl + path + (path.Contains('?') ? "&" : "?") + "culture=" + UserLanguage.Normalize(culture);

        /// <summary>The e-mail settings page (no sign-in): /eposta/… in Turkish, /e-mail/… in German.</summary>
        public static string PreferencesUrl(string token, string? culture) =>
            SeoHelper.SiteUrl + (UserLanguage.IsGerman(culture) ? "/e-mail/" : "/eposta/") + token;

        // ------------------------------------------------------------------
        // Queueing
        // ------------------------------------------------------------------
        public async Task<bool> EnqueueAsync(string template, EmailCategory category, string to, string? userId, string? culture,
            EmailModel model, string dedupeKey, string? relatedType = null, int? relatedId = null, int? campaignId = null, bool wake = true)
        {
            if (string.IsNullOrWhiteSpace(to)) return false;
            if (await _db.EmailOutbox.AnyAsync(o => o.DedupeKey == dedupeKey)) return false;
            var now = DateTime.UtcNow;
            _db.EmailOutbox.Add(new EmailOutbox
            {
                DedupeKey = dedupeKey, Template = template, Category = category, RecipientEmail = to.Trim(), UserId = userId,
                Culture = UserLanguage.Normalize(culture), ModelJson = JsonSerializer.Serialize(model, model.GetType(), Json),
                CreatedAt = now, NextAttemptAt = now, RelatedType = relatedType, RelatedId = relatedId, CampaignId = campaignId
            });
            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("IX_EmailOutbox_DedupeKey") == true)
            {
                return false;   // queued by a parallel request a moment ago
            }
            if (wake) _signal.Wake();
            return true;
        }

        /// <summary>The result of an exam, to the student (always sent).</summary>
        public async Task QueueExamResultAsync(QuizSubmission submission, Quiz quiz, User user, int correct, int total, int answered)
        {
            if (string.IsNullOrWhiteSpace(user.Email)) return;
            try
            {
                var culture = UserLanguage.Normalize(user.PreferredCulture);
                var german = UserLanguage.IsGerman(culture);
                var previousBest = await _db.QuizSubmissions.AsNoTracking()
                    .Where(s => s.UserId == submission.UserId && s.QuizId == quiz.Id && s.QuizSubmissionId != submission.QuizSubmissionId)
                    .MaxAsync(s => (int?)s.Score);
                var model = new ExamResultEmail
                {
                    Name = user.FirstName ?? "",
                    SubmissionId = submission.QuizSubmissionId,
                    QuizTitle = german && !string.IsNullOrWhiteSpace(quiz.TitleDe) ? quiz.TitleDe : quiz.Title ?? "",
                    Level = ExamRules.LevelOf(quiz),
                    Score = submission.Score,
                    Correct = correct,
                    Total = total,
                    Answered = answered,
                    DurationSeconds = submission.DurationSeconds,
                    AttemptNumber = submission.AttemptNumber,
                    PreviousBest = previousBest,
                    EndReason = submission.EndReason ?? "submitted",
                    PassScore = ExamRules.PassScore,
                    ReviewUrl = Link($"/Account/ReviewQuiz/{quiz.Id}?submission={submission.QuizSubmissionId}", culture),
                    QuizzesUrl = Link(german ? "/pruefungen" : "/sinavlar")
                };
                await EnqueueAsync(EmailTemplates.ExamResult, EmailCategory.Transactional, user.Email, user.Id, culture, model,
                    $"result:{submission.QuizSubmissionId}", "submission", submission.QuizSubmissionId);
            }
            catch (Exception ex)
            {
                // The result is saved and shown on the page; a failed e-mail must not undo or hide that
                _logger.LogError(ex, "Could not queue the result e-mail for submission {SubmissionId}", submission.QuizSubmissionId);
            }
        }

        public Task QueueConfirmEmailAsync(User user, string confirmUrl) =>
            EnqueueAsync(EmailTemplates.ConfirmEmail, EmailCategory.Transactional, user.Email ?? "", user.Id, user.PreferredCulture,
                new ConfirmEmailEmail { Name = user.FirstName ?? "", ConfirmUrl = confirmUrl },
                $"confirm:{user.Id}:{DateTime.UtcNow.Ticks}");

        /// <summary>A contact-form message to Suna (Reply-To: the visitor) and a short receipt to the visitor.</summary>
        public async Task QueueContactAsync(string adminEmail, string name, string email, string message, string culture)
        {
            var key = $"contact:{DateTime.UtcNow.Ticks}:{Guid.NewGuid():N}";
            await EnqueueAsync(EmailTemplates.ContactMessage, EmailCategory.Transactional, adminEmail, null, UserLanguage.Turkish,
                new ContactMessageEmail { Name = "Suna", SenderName = name, SenderEmail = email, Message = message, Language = UserLanguage.Normalize(culture) },
                key + ":admin", wake: false);
            await EnqueueAsync(EmailTemplates.ContactReceipt, EmailCategory.Transactional, email, null, culture,
                new ContactReceiptEmail { Name = name }, key + ":receipt");
        }

        /// <summary>
        /// A deleted account: its e-mail log rows lose the address and any kept copy, and its outbox rows go (the
        /// privacy page promises that nothing personal stays behind).
        /// </summary>
        public async Task ForgetUserAsync(string userId)
        {
            await _db.EmailLogs.Where(l => l.UserId == userId)
                .ExecuteUpdateAsync(set => set.SetProperty(l => l.RecipientEmail, "").SetProperty(l => l.Body, (string?)null));
            await _db.EmailOutbox.Where(o => o.UserId == userId).ExecuteDeleteAsync();
        }

        // ------------------------------------------------------------------
        // Announcements
        // ------------------------------------------------------------------
        public static EmailTopics TopicOf(string kind) => kind == EmailTemplates.NewQuiz ? EmailTopics.NewQuizzes : EmailTopics.NewPosts;

        /// <summary>Members who get this kind of announcement: confirmed e-mail, not locked, topic not switched off.</summary>
        public IQueryable<User> Recipients(EmailTopics topic)
        {
            var now = DateTimeOffset.UtcNow;
            var users = _identity.Users.Where(u => u.Email != null && u.EmailConfirmed && (u.LockoutEnd == null || u.LockoutEnd <= now)
                                                   && (u.EmailOptOuts & topic) == 0);
            if (!_options.IncludeLegacyOptIns)
                users = users.Where(u => u.PromoConsentSource != ConsentSource.Legacy);
            return users;
        }

        public Task<int> RecipientCountAsync(string kind) => Recipients(TopicOf(kind)).CountAsync();

        /// <summary>Starts an announcement (once per post/quiz); the worker adds the recipients in batches.</summary>
        public async Task<EmailCampaign?> StartCampaignAsync(string kind, int entityId, string title, string? createdBy)
        {
            if (await _db.EmailCampaigns.AnyAsync(c => c.Kind == kind && c.EntityId == entityId)) return null;
            var campaign = new EmailCampaign
            {
                Kind = kind, EntityId = entityId, Title = title.Length > 300 ? title[..300] : title, CreatedAt = DateTime.UtcNow,
                CreatedBy = createdBy, Status = CampaignStatus.Queuing, Recipients = await RecipientCountAsync(kind)
            };
            _db.EmailCampaigns.Add(campaign);
            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                return null;   // started by a parallel save
            }
            _signal.Wake();
            return campaign;
        }

        public record CampaignSummary(EmailCampaign Campaign, int Sent, int Waiting, int Failed);

        public async Task<CampaignSummary?> CampaignSummaryAsync(string kind, int entityId)
        {
            var campaign = await _db.EmailCampaigns.AsNoTracking().FirstOrDefaultAsync(c => c.Kind == kind && c.EntityId == entityId);
            if (campaign == null) return null;
            return (await SummariesAsync(new[] { campaign }))[0];
        }

        public async Task<List<CampaignSummary>> SummariesAsync(IReadOnlyList<EmailCampaign> campaigns)
        {
            var ids = campaigns.Select(c => c.Id).ToList();
            var sent = await _db.EmailLogs.AsNoTracking().Where(l => l.CampaignId != null && ids.Contains(l.CampaignId.Value))
                .GroupBy(l => new { l.CampaignId, l.Status }).Select(g => new { g.Key.CampaignId, g.Key.Status, N = g.Count() }).ToListAsync();
            var waiting = await _db.EmailOutbox.AsNoTracking().Where(o => o.CampaignId != null && ids.Contains(o.CampaignId.Value) && o.ProcessedAt == null)
                .GroupBy(o => o.CampaignId).Select(g => new { CampaignId = g.Key, N = g.Count() }).ToListAsync();
            return campaigns.Select(c => new CampaignSummary(c,
                sent.Where(x => x.CampaignId == c.Id && x.Status == EmailStatus.Sent).Sum(x => x.N),
                waiting.Where(x => x.CampaignId == c.Id).Sum(x => x.N),
                sent.Where(x => x.CampaignId == c.Id && x.Status == EmailStatus.Failed).Sum(x => x.N))).ToList();
        }

        /// <summary>
        /// Adds the next members (in id order) of a campaign to the outbox. Returns false when everyone is in.
        /// Called by the worker; announcements are written in each member's language.
        /// </summary>
        public async Task<bool> FanOutAsync(EmailCampaign campaign, int batchSize = 200)
        {
            var topic = TopicOf(campaign.Kind);
            var query = Recipients(topic);
            if (campaign.Cursor != null) query = query.Where(u => string.Compare(u.Id, campaign.Cursor) > 0);
            var batch = await query.OrderBy(u => u.Id).Take(batchSize).ToListAsync();   // tracked: tokens may be added

            Func<string, (EmailModel Model, bool Ok)>? build = campaign.Kind switch
            {
                EmailTemplates.NewPost => await NewPostBuilderAsync(campaign.EntityId),
                EmailTemplates.NewQuiz => await NewQuizBuilderAsync(campaign.EntityId),
                _ => null
            };
            if (build == null || batch.Count == 0)
            {
                campaign.Status = build == null ? CampaignStatus.Cancelled : CampaignStatus.Queued;
                await _db.SaveChangesAsync();
                return false;
            }

            foreach (var user in batch)
                if (string.IsNullOrEmpty(user.EmailPreferencesToken))
                    user.EmailPreferencesToken = NewToken();
            await _identity.SaveChangesAsync();

            var now = DateTime.UtcNow;
            var keys = batch.Select(u => $"campaign:{campaign.Id}:{u.Id}").ToList();
            var existing = (await _db.EmailOutbox.Where(o => keys.Contains(o.DedupeKey)).Select(o => o.DedupeKey).ToListAsync()).ToHashSet();
            foreach (var user in batch)
            {
                var key = $"campaign:{campaign.Id}:{user.Id}";
                if (existing.Contains(key)) continue;
                var culture = UserLanguage.Normalize(user.PreferredCulture);
                var (model, ok) = build(culture);
                if (!ok) continue;
                model.Name = user.FirstName ?? "";
                model.PreferencesUrl = PreferencesUrl(user.EmailPreferencesToken!, culture);
                model.UnsubscribeUrl = model.PreferencesUrl;
                model.LegacyConsent = user.PromoConsentSource == ConsentSource.Legacy;
                _db.EmailOutbox.Add(new EmailOutbox
                {
                    DedupeKey = key, Template = campaign.Kind, Category = EmailCategory.Promotional, RecipientEmail = user.Email!,
                    UserId = user.Id, Culture = culture, ModelJson = JsonSerializer.Serialize(model, model.GetType(), Json),
                    CreatedAt = now, NextAttemptAt = now, RelatedType = campaign.Kind == EmailTemplates.NewQuiz ? "quiz" : "blog",
                    RelatedId = campaign.EntityId, CampaignId = campaign.Id
                });
            }
            campaign.Cursor = batch[^1].Id;
            if (batch.Count < batchSize) campaign.Status = CampaignStatus.Queued;
            await _db.SaveChangesAsync();
            return batch.Count == batchSize;
        }

        private async Task<Func<string, (EmailModel, bool)>?> NewPostBuilderAsync(int blogId)
        {
            var blog = await _db.Blogs.AsNoTracking().Include(b => b.Translations).FirstOrDefaultAsync(b => b.BlogId == blogId);
            if (blog == null || !blog.IsPublished) return null;
            var (trPath, dePath) = SeoHelper.BlogPaths(blog);
            var image = string.IsNullOrWhiteSpace(blog.Image) ? null : blog.Image.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? blog.Image : Link("/" + blog.Image.TrimStart('/'));
            return culture =>
            {
                var german = UserLanguage.IsGerman(culture);
                // The member's language when the post exists in it, otherwise the language it was written in
                var useGerman = german ? dePath != null : trPath == null;
                var translation = blog.Translations?.FirstOrDefault(t => t.LanguageCode == (useGerman ? "de" : "tr"));
                var path = useGerman ? dePath : trPath;
                if (path == null) return (new NewPostEmail(), false);
                return (new NewPostEmail
                {
                    Title = FirstText(translation?.Title, blog.Title),
                    Summary = Summarize(FirstText(translation?.Description, blog.Description, blog.MetaDescription)),
                    ImageUrl = image,
                    Url = Link(path)
                }, true);
            };
        }

        private async Task<Func<string, (EmailModel, bool)>?> NewQuizBuilderAsync(int quizId)
        {
            var quiz = await _db.Quizzes.AsNoTracking().Include(q => q.Tags).FirstOrDefaultAsync(q => q.Id == quizId);
            if (quiz == null) return null;
            var questions = await _db.Quizzes.Where(q => q.Id == quizId).Select(q => q.Questions.Count).FirstAsync();
            var image = string.IsNullOrWhiteSpace(quiz.ImageUrl) ? null : quiz.ImageUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? quiz.ImageUrl : Link("/" + quiz.ImageUrl.TrimStart('/'));
            return culture =>
            {
                var german = UserLanguage.IsGerman(culture);
                return (new NewQuizEmail
                {
                    Title = german ? FirstText(quiz.TitleDe, quiz.Title) : FirstText(quiz.Title, quiz.TitleDe),
                    Summary = Summarize(german ? FirstText(quiz.DescriptionDe, quiz.Description) : FirstText(quiz.Description, quiz.DescriptionDe)),
                    Level = ExamRules.LevelOf(quiz),
                    Questions = questions,
                    TimeLimitMinutes = quiz.TimeLimitMinutes,
                    ImageUrl = image,
                    Url = Link($"/Account/StartQuiz/{quiz.Id}", culture)
                }, true);
            };
        }

        private static string FirstText(params string?[] values) => values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v))?.Trim() ?? "";

        /// <summary>Plain text of at most ~240 characters (descriptions may contain HTML from the editor).</summary>
        private static string Summarize(string text)
        {
            var plain = System.Net.WebUtility.HtmlDecode(System.Text.RegularExpressions.Regex.Replace(text, "<[^>]+>", " "));
            plain = System.Text.RegularExpressions.Regex.Replace(plain, @"\s+", " ").Trim();
            if (plain.Length <= 240) return plain;
            var cut = plain.LastIndexOf(' ', 237);
            return plain[..(cut > 120 ? cut : 237)] + "…";
        }

        /// <summary>Random, URL-safe token for the e-mail settings link (43 characters).</summary>
        public static string NewToken() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }
}
