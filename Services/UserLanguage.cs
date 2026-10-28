using System;
using System.Globalization;

namespace SpeakingClub.Services
{
    /// <summary>
    /// The two languages of the site as stored on a user (User.PreferredCulture) and used for their e-mails.
    /// </summary>
    public static class UserLanguage
    {
        public const string Turkish = "tr-TR";
        public const string German = "de-DE";

        /// <summary>The language the current page is shown in.</summary>
        public static string Current =>
            CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "de" ? German : Turkish;

        /// <summary>A stored or requested culture as one of the two; anything unknown is Turkish.</summary>
        public static string Normalize(string? culture) =>
            culture != null && culture.StartsWith("de", StringComparison.OrdinalIgnoreCase) ? German : Turkish;

        public static bool IsGerman(string? culture) => Normalize(culture) == German;
    }
}
