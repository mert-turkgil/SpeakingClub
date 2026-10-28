using System;
using System.Collections.Generic;

namespace SpeakingClub.Services.Email
{
    /// <summary>Template names: Views/Emails/{name}.cshtml.</summary>
    public static class EmailTemplates
    {
        public const string ExamResult = "ExamResult";
        public const string NewPost = "NewPost";
        public const string NewQuiz = "NewQuiz";
        public const string ConfirmEmail = "ConfirmEmail";
        public const string ContactMessage = "ContactMessage";
        public const string ContactReceipt = "ContactReceipt";
        public const string Test = "Test";

        /// <summary>The model type stored (as JSON) with each template in the outbox.</summary>
        public static readonly IReadOnlyDictionary<string, Type> ModelTypes = new Dictionary<string, Type>
        {
            [ExamResult] = typeof(ExamResultEmail),
            [NewPost] = typeof(NewPostEmail),
            [NewQuiz] = typeof(NewQuizEmail),
            [ConfirmEmail] = typeof(ConfirmEmailEmail),
            [ContactMessage] = typeof(ContactMessageEmail),
            [ContactReceipt] = typeof(ContactReceiptEmail),
            [Test] = typeof(TestEmail)
        };

        /// <summary>Templates whose model holds text a visitor wrote: emptied in the outbox once sent (privacy page).</summary>
        public static bool HoldsPersonalText(string template) => template == ContactMessage;
    }

    /// <summary>What every e-mail knows; the links are absolute (e-mails are read outside the site).</summary>
    public abstract class EmailModel
    {
        public string Name { get; set; } = "";
        /// <summary>Announcements only: one-click "no more announcements" (also sent as the List-Unsubscribe header).</summary>
        public string? UnsubscribeUrl { get; set; }
        /// <summary>The member's e-mail settings page (works without signing in).</summary>
        public string? PreferencesUrl { get; set; }
        /// <summary>The member registered before announcements existed and never chose: the e-mail says why it came.</summary>
        public bool LegacyConsent { get; set; }
    }

    public class ExamResultEmail : EmailModel
    {
        public int SubmissionId { get; set; }
        public string QuizTitle { get; set; } = "";
        public string? Level { get; set; }
        public int Score { get; set; }
        public int Correct { get; set; }
        public int Total { get; set; }
        public int Answered { get; set; }
        public int DurationSeconds { get; set; }
        public int AttemptNumber { get; set; }
        /// <summary>Best score at this quiz before this attempt (null on the first attempt).</summary>
        public int? PreviousBest { get; set; }
        public string EndReason { get; set; } = "submitted";
        public int PassScore { get; set; }
        public string ReviewUrl { get; set; } = "";
        public string QuizzesUrl { get; set; } = "";
        public bool Passed => Score >= PassScore;
    }

    public class NewPostEmail : EmailModel
    {
        public string Title { get; set; } = "";
        public string Summary { get; set; } = "";
        public string? ImageUrl { get; set; }
        public string Url { get; set; } = "";
    }

    public class NewQuizEmail : EmailModel
    {
        public string Title { get; set; } = "";
        public string Summary { get; set; } = "";
        public string? Level { get; set; }
        public int Questions { get; set; }
        public int? TimeLimitMinutes { get; set; }
        public string? ImageUrl { get; set; }
        public string Url { get; set; } = "";
    }

    public class ConfirmEmailEmail : EmailModel
    {
        public string ConfirmUrl { get; set; } = "";
    }

    /// <summary>To Suna: a message from the contact form.</summary>
    public class ContactMessageEmail : EmailModel
    {
        public string SenderName { get; set; } = "";
        public string SenderEmail { get; set; } = "";
        public string Message { get; set; } = "";
        public string Language { get; set; } = "";
    }

    /// <summary>To the visitor: "your message arrived" (does not repeat the message).</summary>
    public class ContactReceiptEmail : EmailModel
    {
    }

    public class TestEmail : EmailModel
    {
        public string SentBy { get; set; } = "";
    }

    /// <summary>A rendered e-mail: the subject is set by the template (ViewData["Subject"]).</summary>
    public record RenderedEmail(string Html, string Subject);
}
