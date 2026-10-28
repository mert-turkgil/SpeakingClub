using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SpeakingClub.Data;
using SpeakingClub.Entity;

namespace SpeakingClub.Services.Email
{
    /// <summary>
    /// Sends what is in the outbox: renders the template in the recipient's language, hands it to the mail server and
    /// writes the outcome to EmailLog. Never throws: a failed e-mail is retried later (EmailOutboxService) and, when
    /// it keeps failing, waits in the log for "send again" (Admin → E-postalar).
    /// </summary>
    public class EmailService
    {
        private readonly SpeakingClubContext _db;
        private readonly EmailRenderer _renderer;
        private readonly IMailTransport _transport;
        private readonly EmailOptions _options;
        private readonly ILogger<EmailService> _logger;

        public EmailService(SpeakingClubContext db, EmailRenderer renderer, IMailTransport transport, IOptions<EmailOptions> options, ILogger<EmailService> logger)
        {
            _db = db;
            _renderer = renderer;
            _transport = transport;
            _options = options.Value;
            _logger = logger;
        }

        // Waits between tries of one e-mail: 1 min, 5 min, 15 min, 1 h, 3 h
        private static readonly TimeSpan[] Backoff =
            { TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(15), TimeSpan.FromHours(1), TimeSpan.FromHours(3) };

        /// <summary>Sends one outbox row (tracked by this context) and records the outcome.</summary>
        public async Task<bool> DeliverAsync(EmailOutbox row, CancellationToken ct)
        {
            string? html = null;
            string subject = row.Template;
            EmailModel? model = null;
            try
            {
                if (!EmailTemplates.ModelTypes.TryGetValue(row.Template, out var type))
                    throw new InvalidOperationException($"Unknown e-mail template {row.Template}.");
                model = (EmailModel?)JsonSerializer.Deserialize(row.ModelJson ?? "{}", type, EmailQueue.Json)
                        ?? throw new InvalidOperationException("The e-mail has no content.");
                var rendered = await _renderer.RenderAsync(row.Template, model, row.Culture);
                html = rendered.Html;
                subject = rendered.Subject;
                await _transport.SendAsync(row.RecipientEmail, subject, html,
                    row.Category == EmailCategory.Promotional ? model.UnsubscribeUrl : null,
                    model is ContactMessageEmail contact ? contact.SenderEmail : null, ct);

                row.ProcessedAt = DateTime.UtcNow;
                row.LastError = null;
                if (EmailTemplates.HoldsPersonalText(row.Template)) row.ModelJson = null;   // the message is not kept once delivered
                _db.EmailLogs.Add(NewLog(row, subject, EmailStatus.Sent));
                await _db.SaveChangesAsync(CancellationToken.None);
                return true;
            }
            catch (Exception ex) when (ex is not OperationCanceledException || !ct.IsCancellationRequested)
            {
                row.Attempts++;
                row.LastError = Trim(ex.Message, 1000);
                var final = row.Attempts >= Math.Max(1, _options.MaxAttempts) || html == null && model == null;
                if (final)
                {
                    // Given up: the log row keeps the rendered e-mail so it can be sent again by hand
                    row.ProcessedAt = DateTime.UtcNow;
                    var log = NewLog(row, subject, EmailStatus.Failed);
                    log.Error = row.LastError;
                    log.Body = html;
                    _db.EmailLogs.Add(log);
                    if (EmailTemplates.HoldsPersonalText(row.Template)) row.ModelJson = null;
                }
                else
                {
                    row.NextAttemptAt = DateTime.UtcNow + Backoff[Math.Min(row.Attempts - 1, Backoff.Length - 1)];
                }
                await _db.SaveChangesAsync(CancellationToken.None);
                _logger.LogWarning(ex, "E-mail {Template} to {Recipient} failed (try {Attempt}{Final})", row.Template, row.RecipientEmail,
                    row.Attempts, final ? ", giving up" : "");
                return false;
            }
        }

        private static EmailLog NewLog(EmailOutbox row, string subject, EmailStatus status) => new()
        {
            CreatedAt = row.CreatedAt, Template = row.Template, Category = row.Category, RecipientEmail = row.RecipientEmail,
            UserId = row.UserId, Subject = Trim(subject, 300), Status = status, SentAt = status == EmailStatus.Sent ? DateTime.UtcNow : null,
            RelatedType = row.RelatedType, RelatedId = row.RelatedId, CampaignId = row.CampaignId
        };

        private static string Trim(string text, int max) => text.Length <= max ? text : text[..max];

        public enum ResendOutcome { Sent, FailedAgain, NotFound, NotFailed, AlreadyResent, NoCopy }

        /// <summary>Sends a failed e-mail again, byte for byte. The new try is a new log row; the old one is marked resent.</summary>
        public async Task<(ResendOutcome Outcome, string? Error)> ResendAsync(int logId, CancellationToken ct = default)
        {
            var failed = await _db.EmailLogs.FirstOrDefaultAsync(l => l.Id == logId, ct);
            if (failed == null) return (ResendOutcome.NotFound, null);
            if (failed.Status != EmailStatus.Failed) return (ResendOutcome.NotFailed, null);
            if (failed.ResentAt != null) return (ResendOutcome.AlreadyResent, null);
            if (string.IsNullOrEmpty(failed.Body)) return (ResendOutcome.NoCopy, null);

            var body = failed.Body;
            var attempt = new EmailLog
            {
                CreatedAt = DateTime.UtcNow, Template = failed.Template, Category = failed.Category, RecipientEmail = failed.RecipientEmail,
                UserId = failed.UserId, Subject = failed.Subject, Status = EmailStatus.Queued, RelatedType = failed.RelatedType,
                RelatedId = failed.RelatedId, CampaignId = failed.CampaignId
            };
            failed.ResentAt = DateTime.UtcNow;
            failed.Body = null;
            _db.EmailLogs.Add(attempt);
            await _db.SaveChangesAsync(ct);
            try
            {
                await _transport.SendAsync(attempt.RecipientEmail, attempt.Subject, body, ct: ct);
                attempt.Status = EmailStatus.Sent;
                attempt.SentAt = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                attempt.Status = EmailStatus.Failed;
                attempt.Error = Trim(ex.Message, 1000);
                attempt.Body = body;   // still owed: the new row carries it
                _logger.LogWarning(ex, "Resending e-mail {LogId} failed", logId);
            }
            await _db.SaveChangesAsync(CancellationToken.None);
            return attempt.Status == EmailStatus.Sent ? (ResendOutcome.Sent, null) : (ResendOutcome.FailedAgain, attempt.Error);
        }

        /// <summary>A test e-mail straight to the mail server (Admin → E-postalar): shows at once whether sending works.</summary>
        public async Task<(bool Ok, string? Error)> SendTestAsync(string to, string sentBy, string culture)
        {
            var row = new EmailOutbox
            {
                DedupeKey = "", Template = EmailTemplates.Test, Category = EmailCategory.Transactional, RecipientEmail = to,
                Culture = UserLanguage.Normalize(culture), CreatedAt = DateTime.UtcNow
            };
            var subject = "Test";
            try
            {
                var rendered = await _renderer.RenderAsync(EmailTemplates.Test, new TestEmail { SentBy = sentBy }, row.Culture);
                subject = rendered.Subject;
                await _transport.SendAsync(to, subject, rendered.Html);
                _db.EmailLogs.Add(NewLog(row, subject, EmailStatus.Sent));
                await _db.SaveChangesAsync();
                return (true, null);
            }
            catch (Exception ex)
            {
                var log = NewLog(row, subject, EmailStatus.Failed);
                log.Error = Trim(ex.Message, 1000);
                _db.EmailLogs.Add(log);
                await _db.SaveChangesAsync();
                return (false, ex.Message);
            }
        }

        /// <summary>
        /// "Try the failed ones again": failed e-mails of the last 7 days that still have their copy, oldest first,
        /// at most 50 per click (Gmail's daily limit counts them too).
        /// </summary>
        public async Task<(int Sent, int Failed)> RetryFailedAsync(CancellationToken ct = default)
        {
            var since = DateTime.UtcNow.AddDays(-7);
            var ids = await _db.EmailLogs.Where(l => l.Status == EmailStatus.Failed && l.ResentAt == null && l.Body != null && l.CreatedAt >= since)
                .OrderBy(l => l.Id).Select(l => l.Id).Take(50).ToListAsync(ct);
            int sent = 0, failed = 0;
            foreach (var id in ids)
            {
                var (outcome, _) = await ResendAsync(id, ct);
                if (outcome == ResendOutcome.Sent) sent++; else failed++;
            }
            return (sent, failed);
        }
    }
}
