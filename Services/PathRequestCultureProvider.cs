using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;

namespace SpeakingClub.Services
{
    /// <summary>
    /// Picks the culture from the first URL segment for pages that have a Turkish and a German URL
    /// (/yazilar ↔ /beitraege, /hakkimizda ↔ /ueber-uns, …).
    ///
    /// Without this the language came only from the culture cookie, so a crawler (which has no cookie)
    /// got Turkish content on every German URL, with a canonical pointing back to the Turkish page —
    /// and Google reported all German pages as "Alternate page with proper canonical tag".
    /// Paths that exist in one language only (/, /blog, /Account/…) fall through to the
    /// query string / cookie / Accept-Language providers as before.
    /// </summary>
    public class PathRequestCultureProvider : RequestCultureProvider
    {
        private static readonly Dictionary<string, string> SegmentCultures = new(StringComparer.OrdinalIgnoreCase)
        {
            ["yazilar"] = "tr-TR",
            ["hakkimizda"] = "tr-TR",
            ["gizlilik"] = "tr-TR",
            ["sozluk"] = "tr-TR",
            ["sinavlar"] = "tr-TR",
            ["hekim-almancasi"] = "tr-TR",
            ["hemsirelik-almancasi"] = "tr-TR",

            ["beitraege"] = "de-DE",
            ["ueber-uns"] = "de-DE",
            ["datenschutz"] = "de-DE",
            ["woerterbuch"] = "de-DE",
            ["pruefungen"] = "de-DE",
            ["aerztedeutsch"] = "de-DE",
            ["pflegedeutsch"] = "de-DE",
        };

        public override Task<ProviderCultureResult?> DetermineProviderCultureResult(HttpContext httpContext)
        {
            var path = httpContext.Request.Path.Value;
            if (string.IsNullOrEmpty(path) || path == "/")
                return NullProviderCultureResult;

            var segment = path.AsSpan(1);
            var slash = segment.IndexOf('/');
            if (slash >= 0)
                segment = segment[..slash];

            return SegmentCultures.TryGetValue(segment.ToString(), out var culture)
                ? Task.FromResult<ProviderCultureResult?>(new ProviderCultureResult(culture))
                : NullProviderCultureResult;
        }
    }
}
