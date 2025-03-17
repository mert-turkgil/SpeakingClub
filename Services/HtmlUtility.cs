using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SpeakingClub.Services
{
    public static  class HtmlUtility 
    {
        public static string StripHtml(string source)
        {
            if (string.IsNullOrWhiteSpace(source))
                return string.Empty;
            // Remove HTML tags
            var withoutTags = Regex.Replace(source, "<.*?>", string.Empty);
            // Decode HTML entities if needed (e.g., &amp;)
            // return WebUtility.HtmlDecode(withoutTags);
            return withoutTags;
        }

        public static string GetExcerpt(string source, int maxLength = 100)
        {
            var text = StripHtml(source);
            if (text.Length <= maxLength) return text;
            return text.Substring(0, maxLength) + "...";
        } 
    }
}