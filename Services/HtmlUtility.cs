using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using SpeakingClub.Models;

namespace SpeakingClub.Services
{
    public static  class HtmlUtility
    {
        private static readonly Regex ScriptOrStyle = new(@"<(script|style)\b[^>]*>.*?</\1>", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);
        private static readonly Regex Tag = new("<[^>]*>", RegexOptions.Compiled);
        private static readonly Regex Whitespace = new(@"\s+", RegexOptions.Compiled);
        private static readonly Regex Heading = new(@"<h([23])\b([^>]*)>(.*?)</h\1\s*>", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);
        private static readonly Regex IdAttribute = new(@"\bid\s*=\s*[""']([^""']+)[""']", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex OEmbed = new(@"<oembed\b[^>]*\burl\s*=\s*[""']([^""']+)[""'][^>]*>\s*(?:</oembed>)?", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex YouTubeId = new(@"(?:youtube(?:-nocookie)?\.com/(?:watch\?(?:[^#]*?&(?:amp;)?)?v=|embed/|shorts/|live/)|youtu\.be/)([A-Za-z0-9_-]{11})", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex VimeoId = new(@"vimeo\.com/(?:video/)?(\d+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>
        /// Plain text of an HTML fragment: tags removed, entities decoded (&amp;nbsp;, &amp;ouml;, …), whitespace collapsed.
        /// The result is text, not HTML — render it encoded (@value), never with Html.Raw.
        /// </summary>
        public static string StripHtml(string source)
        {
            if (string.IsNullOrWhiteSpace(source))
                return string.Empty;
            var withoutScripts = ScriptOrStyle.Replace(source, " ");
            // Tags become spaces so "</p><p>" does not glue two words together
            var withoutTags = Tag.Replace(withoutScripts, " ");
            return Whitespace.Replace(WebUtility.HtmlDecode(withoutTags), " ").Trim();
        }

        /// <summary>
        /// Plain-text excerpt of at most <paramref name="maxLength"/> characters, cut at a word boundary.
        /// </summary>
        public static string GetExcerpt(string source, int maxLength = 100)
        {
            var text = StripHtml(source);
            if (text.Length <= maxLength) return text;
            var cut = text.LastIndexOf(' ', maxLength - 1);
            if (cut < maxLength / 2) cut = maxLength - 1;
            return text[..cut].TrimEnd(' ', ',', ';', ':', '.', '-') + "…";
        }

        /// <summary>
        /// Estimated reading time in minutes (200 words per minute, at least 1).
        /// </summary>
        public static int ReadingMinutes(string html)
        {
            var words = StripHtml(html).Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
            return Math.Max(1, (int)Math.Round(words / 200.0));
        }

        /// <summary>
        /// Prepares CKEditor HTML for the public article page:
        /// - h1 → h2, because the page title is the page's only h1 (CKEditor's Title plugin writes an h1);
        /// - h2/h3 get stable anchor ids, collected into <paramref name="tableOfContents"/>;
        /// - images load lazily;
        /// - &lt;oembed&gt; media (which browsers cannot display) becomes a YouTube/Vimeo player, or a link.
        /// </summary>
        public static string PrepareArticleHtml(string html, List<TocItem> tableOfContents)
        {
            if (string.IsNullOrWhiteSpace(html))
                return string.Empty;

            html = Regex.Replace(html, @"<(/?)h1\b", "<$1h2", RegexOptions.IgnoreCase);

            var usedIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            html = Heading.Replace(html, match =>
            {
                var level = int.Parse(match.Groups[1].Value);
                var attributes = match.Groups[2].Value;
                var inner = match.Groups[3].Value;
                var text = StripHtml(inner);
                if (text.Length == 0)
                    return match.Value;

                var existingId = IdAttribute.Match(attributes);
                string id;
                if (existingId.Success)
                {
                    id = existingId.Groups[1].Value;
                }
                else
                {
                    id = UniqueId(AnchorSlug(text), usedIds);
                    attributes += $" id=\"{id}\"";
                }
                usedIds.Add(id);
                tableOfContents.Add(new TocItem { Level = level, Id = id, Text = text });
                return $"<h{level}{attributes}>{inner}</h{level}>";
            });

            html = Regex.Replace(html, @"<img\b(?![^>]*\bloading\s*=)", "<img loading=\"lazy\" decoding=\"async\"", RegexOptions.IgnoreCase);

            html = OEmbed.Replace(html, match => EmbedFor(match.Groups[1].Value));

            return html;
        }

        private static string EmbedFor(string encodedUrl)
        {
            var url = WebUtility.HtmlDecode(encodedUrl);

            var youTube = YouTubeId.Match(url);
            if (youTube.Success)
                return Player($"https://www.youtube-nocookie.com/embed/{youTube.Groups[1].Value}", "YouTube video");

            var vimeo = VimeoId.Match(url);
            if (vimeo.Success)
                return Player($"https://player.vimeo.com/video/{vimeo.Groups[1].Value}", "Vimeo video");

            var safeUrl = WebUtility.HtmlEncode(url);
            return $"<a href=\"{safeUrl}\" target=\"_blank\" rel=\"noopener noreferrer\">{safeUrl}</a>";
        }

        private static string Player(string src, string title) =>
            $"<span class=\"media-embed\"><iframe src=\"{src}\" title=\"{title}\" loading=\"lazy\" " +
            "allow=\"accelerometer; clipboard-write; encrypted-media; gyroscope; picture-in-picture; web-share\" " +
            "referrerpolicy=\"strict-origin-when-cross-origin\" allowfullscreen></iframe></span>";

        // Anchor ids that read well in a URL: "Hasta Devir Cümleleri (Übergabe)" → "hasta-devir-cumleleri-ubergabe".
        // GenerateSlug handles the Turkish letters (ö and ü included); ä and ß only occur in German.
        private static string AnchorSlug(string text)
        {
            var german = text.ToLowerInvariant().Replace("ä", "ae").Replace("ß", "ss");
            var slug = SeoHelper.GenerateSlug(german);
            return slug.Length > 60 ? slug[..60].TrimEnd('-') : slug;
        }

        private static string UniqueId(string slug, HashSet<string> usedIds)
        {
            if (string.IsNullOrEmpty(slug)) slug = "bolum";
            var id = slug;
            for (var n = 2; usedIds.Contains(id); n++)
                id = $"{slug}-{n}";
            return id;
        }
    }
}
