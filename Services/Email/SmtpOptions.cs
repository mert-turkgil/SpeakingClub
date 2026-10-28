using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;

namespace SpeakingClub.Services.Email
{
    /// <summary>
    /// The mail server, bound from the "EmailSender" section (the keys the deploy workflow already writes:
    /// SMTPMail, Port, Username, Password, FromEmail, FromName, ReplyToEmail). Username and Password are secrets:
    /// user-secrets or appsettings.Development.json locally, GitHub secrets in the deploy; never committed.
    /// </summary>
    public class SmtpOptions
    {
        public const string Section = "EmailSender";

        [ConfigurationKeyName("SMTPMail")]
        public string Host { get; set; } = "";
        public int Port { get; set; } = 587;
        public bool UseSsl { get; set; } = true;
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
        public string FromEmail { get; set; } = "";
        public string FromName { get; set; } = "Almanca Konuşma Kulübü";
        /// <summary>Where a reply lands (Suna's own mailbox), not the sending address.</summary>
        public string ReplyToEmail { get; set; } = "";
        public int TimeoutSeconds { get; set; } = 30;

        /// <summary>The sender address: FromEmail, or the signed-in mailbox when that is empty.</summary>
        public string Sender => string.IsNullOrWhiteSpace(FromEmail) ? Username : FromEmail;

        public bool IsConfigured => !string.IsNullOrWhiteSpace(Host) && !string.IsNullOrWhiteSpace(Sender);

        /// <summary>
        /// What is wrong with these settings, in words an admin can act on; empty when they look usable. Shown on
        /// Admin → E-postalar and logged at start, because a broken mailer otherwise fails one e-mail at a time.
        /// </summary>
        public IReadOnlyList<string> Problems()
        {
            var problems = new List<string>();
            if (string.IsNullOrWhiteSpace(Host)) problems.Add("EmailSender:SMTPMail is empty: no mail server is set.");
            if (string.IsNullOrWhiteSpace(Sender)) problems.Add("EmailSender:FromEmail and EmailSender:Username are empty: e-mails need a sender.");
            if (Port <= 0) problems.Add($"EmailSender:Port {Port} is not a valid port.");
            if (!string.IsNullOrWhiteSpace(Username) && string.IsNullOrEmpty(Password))
                problems.Add("EmailSender:Username is set but EmailSender:Password is empty: Gmail needs an app password.");
            // Gmail only sends as the signed-in mailbox (or an alias added in Gmail's settings)
            if (!string.IsNullOrWhiteSpace(Username) && !string.IsNullOrWhiteSpace(FromEmail) && Username.Contains('@')
                && !string.Equals(Username.Trim(), FromEmail.Trim(), StringComparison.OrdinalIgnoreCase))
                problems.Add($"EmailSender:FromEmail ({FromEmail}) differs from EmailSender:Username ({Username}). Gmail rewrites or rejects that unless it is an alias of the mailbox.");
            return problems;
        }
    }

    /// <summary>How fast e-mails go out, bound from the "Email" section (all optional).</summary>
    public class EmailOptions
    {
        public const string Section = "Email";

        /// <summary>Off: nothing is sent (the queue keeps filling); for a site copy that must stay quiet.</summary>
        public bool Enabled { get; set; } = true;
        /// <summary>Announcements per minute (exam results and sign-up mails never wait for this).</summary>
        public int PerMinute { get; set; } = 6;
        /// <summary>
        /// Most e-mails sent per day (Turkish day), all kinds together. A personal Gmail account allows about 500;
        /// announcements stop at this number and continue the next day.
        /// </summary>
        public int DailyCap { get; set; } = 400;
        /// <summary>Tries before an e-mail is marked failed (it can still be resent from the admin page).</summary>
        public int MaxAttempts { get; set; } = 6;
        /// <summary>
        /// Members who registered before the announcements existed count as subscribed (the site owner's decision).
        /// Setting this to false limits announcements to people who said yes themselves.
        /// </summary>
        public bool IncludeLegacyOptIns { get; set; } = true;

        /// <summary>Where contact-form messages go (Suna's mailbox).</summary>
        public string ContactTo { get; set; } = "almancakonus.info@gmail.com";
    }
}
