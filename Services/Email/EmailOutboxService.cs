using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SpeakingClub.Data;
using SpeakingClub.Entity;
using SpeakingClub.Identity;
using SpeakingClub.Services.Reports;

namespace SpeakingClub.Services.Email
{
    /// <summary>
    /// The e-mail worker. Every few seconds (or at once when something is queued) it
    ///  1. adds the next members of running announcements to the outbox (200 at a time),
    ///  2. sends what is due: exam results, sign-up and contact mails first and right away; announcements at
    ///     Email:PerMinute and only while today's total is under Email:DailyCap (Gmail allows about 500 a day),
    ///  3. every 5 minutes collects exams whose time ran out while the student was away, so their result e-mail
    ///     goes out too (before, they were collected only when the student came back).
    /// IIS stops an idle site: then the worker pauses and continues with the next visit (see README, "uptime ping").
    /// </summary>
    public class EmailOutboxService : BackgroundService
    {
        private static readonly TimeSpan Tick = TimeSpan.FromSeconds(10);
        private static readonly TimeSpan ExpiredSweep = TimeSpan.FromMinutes(5);

        private readonly IServiceScopeFactory _scopes;
        private readonly EmailSignal _signal;
        private readonly EmailOptions _options;
        private readonly ILogger<EmailOutboxService> _logger;
        private DateTime _lastSweep = DateTime.MinValue;

        public EmailOutboxService(IServiceScopeFactory scopes, EmailSignal signal, IOptions<EmailOptions> options,
            IOptions<SmtpOptions> smtp, ILogger<EmailOutboxService> logger)
        {
            _scopes = scopes;
            _signal = signal;
            _options = options.Value;
            _logger = logger;
            foreach (var problem in smtp.Value.Problems())
                logger.LogWarning("E-mail settings: {Problem}", problem);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);   // let the site start first
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await RunOnceAsync(stoppingToken);
                }
                catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
                {
                    _logger.LogError(ex, "E-mail worker round failed");
                }
                try
                {
                    await _signal.WaitAsync(Tick, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        private async Task RunOnceAsync(CancellationToken ct)
        {
            if (DateTime.UtcNow - _lastSweep >= ExpiredSweep)
            {
                _lastSweep = DateTime.UtcNow;
                await CollectExpiredExamsAsync(ct);
            }
            if (!_options.Enabled) return;

            using var scope = _scopes.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<SpeakingClubContext>();
            var queue = scope.ServiceProvider.GetRequiredService<EmailQueue>();
            var mail = scope.ServiceProvider.GetRequiredService<EmailService>();

            // 1. Announcements: add the next recipients
            foreach (var campaign in await db.EmailCampaigns.Where(c => c.Status == CampaignStatus.Queuing).OrderBy(c => c.Id).Take(3).ToListAsync(ct))
                await queue.FanOutAsync(campaign);

            // 2. Send. Results, confirmations and contact messages never wait for the pace or the daily cap
            var now = DateTime.UtcNow;
            var urgent = await db.EmailOutbox
                .Where(o => o.ProcessedAt == null && o.Category == EmailCategory.Transactional && o.NextAttemptAt <= now)
                .OrderBy(o => o.NextAttemptAt).Take(20).ToListAsync(ct);
            foreach (var row in urgent)
                await mail.DeliverAsync(row, ct);

            var dayStart = ReportTime.StartOfDayUtc(ReportTime.Today);
            var sentToday = await db.EmailLogs.CountAsync(l => l.SentAt >= dayStart, ct);
            var lastMinute = DateTime.UtcNow.AddMinutes(-1);
            var promoLastMinute = await db.EmailLogs.CountAsync(l => l.Category == EmailCategory.Promotional && l.SentAt >= lastMinute, ct);
            // Spread over the minute (6 per minute = one every 10 s) rather than in one burst, which Gmail likes better
            var perTick = (int)Math.Ceiling(Math.Max(0, _options.PerMinute) * Tick.TotalSeconds / 60);
            var allowance = Math.Min(Math.Min(perTick, _options.PerMinute - promoLastMinute), _options.DailyCap - sentToday);
            if (allowance <= 0) return;

            var promotional = await db.EmailOutbox
                .Where(o => o.ProcessedAt == null && o.Category == EmailCategory.Promotional && o.NextAttemptAt <= now)
                .OrderBy(o => o.NextAttemptAt).ThenBy(o => o.Id).Take(allowance).ToListAsync(ct);
            foreach (var row in promotional)
            {
                if (ct.IsCancellationRequested) break;
                await mail.DeliverAsync(row, ct);
            }
        }

        private async Task CollectExpiredExamsAsync(CancellationToken ct)
        {
            try
            {
                using var scope = _scopes.CreateScope();
                var exams = scope.ServiceProvider.GetRequiredService<ExamService>();
                var users = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
                var collected = await exams.FinalizeAllExpiredAsync(id => users.FindByIdAsync(id));
                if (collected > 0) _logger.LogInformation("Collected {Count} exams whose time ran out", collected);
            }
            catch (Exception ex) when (!ct.IsCancellationRequested)
            {
                _logger.LogError(ex, "Collecting expired exams failed");
            }
        }
    }
}
