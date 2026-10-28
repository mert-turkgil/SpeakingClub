using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using SpeakingClub.Services.Email;

namespace SpeakingClub.Identity
{
    public class User : IdentityUser
    {
        public const int NameMaxLength = 100;

        #nullable disable
        public string LastName { get; set; }
        public string FirstName { get; set; }
        public int Age { get; set; }

        // UTC. Null for accounts made before this was recorded and with no activity to estimate it from.
        public DateTime? CreatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }

        // "tr-TR" or "de-DE": the language e-mails are written in
        public string PreferredCulture { get; set; }

        // Announcement topics (new posts, new quizzes) the member switched off. Stored as opt-outs, so accounts
        // that existed before the e-mails started receive them (the site owner's decision; source "legacy");
        // new sign-ups opt in with a checkbox. Exam results are always sent.
        public EmailTopics EmailOptOuts { get; set; }
        public DateTime? PromoConsentAt { get; set; }
        // legacy, register, google, account, admin, unsubscribe (Services/Email/EmailTopics.cs → ConsentSource)
        public string PromoConsentSource { get; set; }

        // Random token for the e-mail preferences / one-click unsubscribe link (no sign-in needed)
        public string EmailPreferencesToken { get; set; }
    }
}