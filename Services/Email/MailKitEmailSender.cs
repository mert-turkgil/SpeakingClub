using System;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace SpeakingClub.Services.Email
{
    /// <summary>Hands one finished e-mail to the mail server.</summary>
    public interface IMailTransport
    {
        Task SendAsync(string to, string subject, string html, string? unsubscribeUrl = null, string? replyTo = null, CancellationToken ct = default);
    }

    /// <summary>
    /// SMTP through MailKit (System.Net.Mail is no longer developed). Sends HTML plus a plain-text copy, and the
    /// one-click unsubscribe headers Gmail and Yahoo require for announcements (RFC 8058). Errors are rewritten in
    /// words that say what to fix, because they end up in the e-mail log on the admin page.
    /// </summary>
    public partial class MailKitEmailSender : IMailTransport
    {
        private readonly SmtpOptions _options;

        public MailKitEmailSender(IOptions<SmtpOptions> options) => _options = options.Value;

        public async Task SendAsync(string to, string subject, string html, string? unsubscribeUrl = null, string? replyTo = null, CancellationToken ct = default)
        {
            if (!_options.IsConfigured)
                throw new InvalidOperationException("E-mail is not set up: " + string.Join(" ", _options.Problems()));

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_options.FromName, _options.Sender));
            message.To.Add(MailboxAddress.Parse(to));
            // Replies go to Suna's own mailbox; a contact-form message replies to the visitor who wrote it
            var reply = !string.IsNullOrWhiteSpace(replyTo) ? replyTo : _options.ReplyToEmail;
            if (!string.IsNullOrWhiteSpace(reply) && MailboxAddress.TryParse(reply, out var replyAddress))
                message.ReplyTo.Add(replyAddress);
            message.Subject = subject;
            if (unsubscribeUrl != null)
            {
                message.Headers.Add("List-Unsubscribe", $"<{unsubscribeUrl}>");
                message.Headers.Add("List-Unsubscribe-Post", "List-Unsubscribe=One-Click");
            }
            message.Body = new BodyBuilder { HtmlBody = html, TextBody = ToPlainText(html) }.ToMessageBody();

            using var client = new SmtpClient { Timeout = Math.Max(5, _options.TimeoutSeconds) * 1000 };
            // 465 is TLS from the first byte; 587 (Gmail) starts plain and upgrades. Mixing them up just hangs.
            var socket = _options.Port == 465 ? SecureSocketOptions.SslOnConnect
                : _options.UseSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None;
            try
            {
                await client.ConnectAsync(_options.Host, _options.Port, socket, ct);
                if (!string.IsNullOrEmpty(_options.Username))
                    await client.AuthenticateAsync(_options.Username, _options.Password, ct);
                await client.SendAsync(message, ct);
            }
            catch (AuthenticationException ex)
            {
                throw new InvalidOperationException(
                    $"The mail server ({_options.Host}) refused the sign-in for {_options.Username}: {ex.Message} With Gmail this needs an app password (Google account → Security → App passwords).", ex);
            }
            catch (SmtpCommandException ex) when (ex.ErrorCode == SmtpErrorCode.RecipientNotAccepted)
            {
                throw new InvalidOperationException($"The mail server did not accept the address {to}: {ex.Message}", ex);
            }
            catch (SmtpCommandException ex) when (ex.StatusCode is SmtpStatusCode.ServiceNotAvailable or SmtpStatusCode.MailboxBusy
                                                   || ex.Message.Contains("limit", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"The mail server is busy or the daily sending limit was reached ({(int)ex.StatusCode} {ex.Message}). The e-mail is tried again later.", ex);
            }
            finally
            {
                if (client.IsConnected) await client.DisconnectAsync(true, CancellationToken.None);
            }
        }

        /// <summary>
        /// A readable text version of the HTML: hidden preheader and styles dropped, links as "label (url)", blocks
        /// as line breaks. Spam filters score HTML-only mail worse, and some readers prefer text.
        /// </summary>
        public static string ToPlainText(string html)
        {
            var text = HiddenBlocks().Replace(html, "");
            text = Anchors().Replace(text, m =>
            {
                var href = m.Groups["href"].Value;
                var label = Tags().Replace(m.Groups["label"].Value, "").Trim();
                if (href.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase)) return label;
                return string.IsNullOrEmpty(label) || label == href ? href : $"{label} ({href})";
            });
            text = CellEnds().Replace(text, " ");   // "Doğru 9 / 14", not "Doğru9 / 14"
            text = LineBreaks().Replace(text, "\n");
            text = Tags().Replace(text, "");
            text = WebUtility.HtmlDecode(text);
            text = HorizontalSpace().Replace(text, " ");
            text = BlankLines().Replace(text, "\n\n");
            return string.Join('\n', text.Split('\n').Select(l => l.Trim())).Trim();
        }

        [GeneratedRegex(@"<(head|style|script)\b[^>]*>.*?</\1>|<div[^>]*display:\s*none[^>]*>.*?</div>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
        private static partial Regex HiddenBlocks();

        [GeneratedRegex(@"<a\b[^>]*href=[""'](?<href>[^""']+)[""'][^>]*>(?<label>.*?)</a>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
        private static partial Regex Anchors();

        [GeneratedRegex(@"<br\s*/?>|</(p|tr|h[1-6]|li|div|table)>", RegexOptions.IgnoreCase)]
        private static partial Regex LineBreaks();

        [GeneratedRegex(@"</t[dh]>", RegexOptions.IgnoreCase)]
        private static partial Regex CellEnds();

        [GeneratedRegex(@"<[^>]+>")]
        private static partial Regex Tags();

        [GeneratedRegex(@"[ \t ]+")]
        private static partial Regex HorizontalSpace();

        [GeneratedRegex(@"\n\s*\n(\s*\n)+")]
        private static partial Regex BlankLines();
    }
}
