using System;

namespace SpeakingClub.Entity
{
    // E-mails (Services/Email): every e-mail goes through the outbox, is sent by EmailOutboxService and leaves a row in
    // EmailLog. Announcements to many members are an EmailCampaign that the service turns into outbox rows bit by bit.
    // All dates UTC.

    public enum EmailCategory
    {
        Transactional = 0,   // exam result, sign-up confirmation, contact form: always sent
        Promotional = 1      // announcements (new post, new quiz): only to members who did not switch them off
    }

    public enum EmailStatus
    {
        Queued = 0,
        Sent = 1,
        Failed = 2
    }

    /// <summary>
    /// One e-mail that was sent or failed (Admin → E-postalar). A sent e-mail keeps the address and subject only;
    /// the body is kept just while a failed one waits to be resent, so confirmation links are not stored.
    /// Purged after 180 days (TempFileCleanupService).
    /// </summary>
    public class EmailLog
    {
        public int Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Template { get; set; } = "";
        public EmailCategory Category { get; set; }
        public string RecipientEmail { get; set; } = "";
        public string? UserId { get; set; }
        public string Subject { get; set; } = "";
        public EmailStatus Status { get; set; }
        public string? Error { get; set; }
        public DateTime? SentAt { get; set; }
        /// <summary>The rendered HTML of a failed e-mail, for "send again"; cleared once resent.</summary>
        public string? Body { get; set; }
        public DateTime? ResentAt { get; set; }
        public string? RelatedType { get; set; }   // "submission", "blog", "quiz"
        public int? RelatedId { get; set; }
        public int? CampaignId { get; set; }
    }

    /// <summary>
    /// An e-mail waiting to be sent. The model is stored as JSON and rendered when it goes out, in the recipient's
    /// language. DedupeKey makes enqueueing idempotent ("result:123", "campaign:4:userId").
    /// </summary>
    public class EmailOutbox
    {
        public int Id { get; set; }
        public string DedupeKey { get; set; } = "";
        public string Template { get; set; } = "";
        public EmailCategory Category { get; set; }
        public string RecipientEmail { get; set; } = "";
        public string? UserId { get; set; }
        public string Culture { get; set; } = "tr-TR";
        /// <summary>The template's model; emptied after sending when it holds personal text (contact form).</summary>
        public string? ModelJson { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime NextAttemptAt { get; set; }
        public int Attempts { get; set; }
        public DateTime? ProcessedAt { get; set; }
        public string? LastError { get; set; }
        public string? RelatedType { get; set; }
        public int? RelatedId { get; set; }
        public int? CampaignId { get; set; }
    }

    public enum CampaignStatus
    {
        Queuing = 0,     // members are still being added to the outbox
        Queued = 1,      // everyone is in the outbox; sending continues at the allowed pace
        Cancelled = 2
    }

    /// <summary>An announcement of a new post or quiz to the members who want them (one per post/quiz).</summary>
    public class EmailCampaign
    {
        public int Id { get; set; }
        public string Kind { get; set; } = "";        // "NewPost" | "NewQuiz" (template name)
        public int EntityId { get; set; }             // BlogId or Quiz Id
        public string Title { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public string? CreatedBy { get; set; }
        public CampaignStatus Status { get; set; }
        /// <summary>Last AspNetUsers.Id added to the outbox (members are added in id order, 200 at a time).</summary>
        public string? Cursor { get; set; }
        public int Recipients { get; set; }
    }
}
