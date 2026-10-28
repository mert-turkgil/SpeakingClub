using System;

namespace SpeakingClub.Services.Email
{
    /// <summary>
    /// Announcement e-mails a member can switch off (User.EmailOptOuts stores the switched-off ones).
    /// Exam results are not a topic: they are always sent.
    /// </summary>
    [Flags]
    public enum EmailTopics
    {
        None = 0,
        NewPosts = 1,
        NewQuizzes = 2,
        All = NewPosts | NewQuizzes
    }

    /// <summary>Where a member's choice about announcements came from (User.PromoConsentSource).</summary>
    public static class ConsentSource
    {
        // Accounts that existed before the e-mails started: counted as subscribed (the site owner's decision)
        public const string Legacy = "legacy";
        public const string Register = "register";
        public const string Google = "google";
        public const string Account = "account";
        public const string Admin = "admin";
        public const string Unsubscribe = "unsubscribe";
    }
}
