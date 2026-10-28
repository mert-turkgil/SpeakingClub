using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SpeakingClub.Entity;
using SpeakingClub.Services.Email;
using SpeakingClub.Services.Reports;

namespace SpeakingClub.Controllers
{
    // Admin → E-postalar (Admin, Root): mail server, today's count, announcements, the send log with "send again".
    public partial class AdminController
    {
        public record EmailsPage(
            SmtpOptions Smtp, EmailOptions Options, IReadOnlyList<string> Problems,
            int SentToday, int WaitingTransactional, int WaitingPromotional, int FailedOpen,
            IReadOnlyList<EmailQueue.CampaignSummary> Campaigns,
            string Status, IReadOnlyList<EmailLog> Logs, IReadOnlyList<EmailOutbox> Waiting, int Page, int TotalPages);

        private const int EmailPageSize = 50;

        [HttpGet("Emails"), Authorize(Roles = "Admin,Root")]
        public async Task<IActionResult> Emails(string? status, int page = 1, [FromServices] IOptions<SmtpOptions> smtp = null!,
            [FromServices] IOptions<EmailOptions> options = null!, [FromServices] EmailQueue queue = null!)
        {
            status = status is "sent" or "failed" or "queued" ? status : "all";
            var dayStart = ReportTime.StartOfDayUtc(ReportTime.Today);
            var waiting = _db.EmailOutbox.AsNoTracking().Where(o => o.ProcessedAt == null);

            IReadOnlyList<EmailLog> logs = Array.Empty<EmailLog>();
            IReadOnlyList<EmailOutbox> queued = Array.Empty<EmailOutbox>();
            int total;
            if (status == "queued")
            {
                total = await waiting.CountAsync();
                page = Math.Clamp(page, 1, Math.Max(1, (int)Math.Ceiling(total / (double)EmailPageSize)));
                queued = await waiting.OrderBy(o => o.Category).ThenBy(o => o.NextAttemptAt)
                    .Skip((page - 1) * EmailPageSize).Take(EmailPageSize).ToListAsync();
            }
            else
            {
                var query = _db.EmailLogs.AsNoTracking();
                if (status == "sent") query = query.Where(l => l.Status == EmailStatus.Sent);
                if (status == "failed") query = query.Where(l => l.Status == EmailStatus.Failed);
                total = await query.CountAsync();
                page = Math.Clamp(page, 1, Math.Max(1, (int)Math.Ceiling(total / (double)EmailPageSize)));
                // Without the kept bodies: the list only needs to know whether one exists
                logs = await query.OrderByDescending(l => l.Id).Skip((page - 1) * EmailPageSize).Take(EmailPageSize)
                    .Select(l => new EmailLog
                    {
                        Id = l.Id, CreatedAt = l.CreatedAt, Template = l.Template, Category = l.Category, RecipientEmail = l.RecipientEmail,
                        UserId = l.UserId, Subject = l.Subject, Status = l.Status, Error = l.Error, SentAt = l.SentAt,
                        Body = l.Body != null ? "" : null, ResentAt = l.ResentAt, RelatedType = l.RelatedType, RelatedId = l.RelatedId,
                        CampaignId = l.CampaignId
                    }).ToListAsync();
            }

            var campaigns = await _db.EmailCampaigns.AsNoTracking().OrderByDescending(c => c.Id).Take(8).ToListAsync();
            return View("Emails", new EmailsPage(
                smtp.Value, options.Value, smtp.Value.Problems(),
                await _db.EmailLogs.CountAsync(l => l.SentAt >= dayStart),
                await waiting.CountAsync(o => o.Category == EmailCategory.Transactional),
                await waiting.CountAsync(o => o.Category == EmailCategory.Promotional),
                await _db.EmailLogs.CountAsync(l => l.Status == EmailStatus.Failed && l.ResentAt == null && l.CreatedAt >= DateTime.UtcNow.AddDays(-7)),
                await queue.SummariesAsync(campaigns),
                status, logs, queued, page, Math.Max(1, (int)Math.Ceiling(total / (double)EmailPageSize))));
        }

        [HttpPost("Emails/test"), Authorize(Roles = "Admin,Root"), ValidateAntiForgeryToken]
        public async Task<IActionResult> EmailTest(string? to, [FromServices] EmailService mail)
        {
            var me = await _userManager.GetUserAsync(User);
            var address = string.IsNullOrWhiteSpace(to) ? me?.Email : to.Trim();
            if (string.IsNullOrWhiteSpace(address) || !address.Contains('@'))
            {
                TempData["ErrorMessage"] = T("Geçerli bir e-posta adresi yazın.", "Bitte eine gültige E-Mail-Adresse eingeben.");
                return RedirectToAction(nameof(Emails));
            }
            var name = me == null ? "Admin" : Services.ExamService.FullName(me);
            var (ok, error) = await mail.SendTestAsync(address, string.IsNullOrWhiteSpace(name) ? me?.Email ?? "Admin" : name, Services.UserLanguage.Current);
            if (ok) TempData["SuccessMessage"] = T($"Deneme e-postası {address} adresine gönderildi. Gelen kutusuna (ve spam klasörüne) bakın.", $"Test-E-Mail an {address} gesendet. Bitte Posteingang (und Spam) prüfen.");
            else TempData["ErrorMessage"] = T("Deneme e-postası gönderilemedi: ", "Test-E-Mail konnte nicht gesendet werden: ") + error;
            return RedirectToAction(nameof(Emails));
        }

        [HttpPost("Emails/retry"), Authorize(Roles = "Admin,Root"), ValidateAntiForgeryToken]
        public async Task<IActionResult> EmailRetry([FromServices] EmailService mail)
        {
            var (sent, failed) = await mail.RetryFailedAsync(HttpContext.RequestAborted);
            if (sent + failed == 0) TempData["SuccessMessage"] = T("Tekrar denenecek e-posta yok.", "Keine E-Mails zum erneuten Versuch.");
            else if (failed == 0) TempData["SuccessMessage"] = T($"{sent} e-posta gönderildi.", $"{sent} E-Mails gesendet.");
            else TempData["ErrorMessage"] = T($"{sent} e-posta gönderildi, {failed} tanesi yine gönderilemedi.", $"{sent} E-Mails gesendet, {failed} weiterhin nicht zustellbar.");
            return RedirectToAction(nameof(Emails), new { status = "failed" });
        }

        [HttpPost("Emails/resend/{id:int}"), Authorize(Roles = "Admin,Root"), ValidateAntiForgeryToken]
        public async Task<IActionResult> EmailResend(int id, [FromServices] EmailService mail)
        {
            var (outcome, error) = await mail.ResendAsync(id, HttpContext.RequestAborted);
            switch (outcome)
            {
                case EmailService.ResendOutcome.Sent:
                    TempData["SuccessMessage"] = T("E-posta tekrar gönderildi.", "Die E-Mail wurde erneut gesendet.");
                    break;
                case EmailService.ResendOutcome.FailedAgain:
                    TempData["ErrorMessage"] = T("Yine gönderilemedi: ", "Wieder nicht zugestellt: ") + error;
                    break;
                default:
                    TempData["ErrorMessage"] = T("Bu e-posta tekrar gönderilemez (zaten gönderildi ya da kopyası yok).", "Diese E-Mail kann nicht erneut gesendet werden (schon gesendet oder keine Kopie).");
                    break;
            }
            return RedirectToAction(nameof(Emails), new { status = "failed" });
        }

        /// <summary>Stops an announcement: e-mails not sent yet are dropped; the ones already sent stay in the log.</summary>
        [HttpPost("Emails/campaign/{id:int}/stop"), Authorize(Roles = "Admin,Root"), ValidateAntiForgeryToken]
        public async Task<IActionResult> EmailCampaignStop(int id)
        {
            var campaign = await _db.EmailCampaigns.FirstOrDefaultAsync(c => c.Id == id);
            if (campaign != null)
            {
                campaign.Status = CampaignStatus.Cancelled;
                await _db.SaveChangesAsync();
                var dropped = await _db.EmailOutbox.Where(o => o.CampaignId == id && o.ProcessedAt == null).ExecuteDeleteAsync();
                TempData["SuccessMessage"] = T($"Duyuru durduruldu; {dropped} e-posta gönderilmeyecek.", $"Ankündigung angehalten; {dropped} E-Mails werden nicht gesendet.");
            }
            return RedirectToAction(nameof(Emails));
        }
    }
}
