using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SpeakingClub.Data;
using SpeakingClub.Services;

namespace SpeakingClub.Controllers
{
    public class SitemapController : Controller
    {
        private readonly SpeakingClubContext _context;
        private readonly ILogger<SitemapController> _logger;
        private const string SITE_URL = SeoHelper.SiteUrl;

        private static readonly XNamespace Ns = "http://www.sitemaps.org/schemas/sitemap/0.9";
        private static readonly XNamespace NsXhtml = "http://www.w3.org/1999/xhtml";

        // Pages that exist as a Turkish/German pair: (Turkish path, German path, priority, changefreq).
        private static readonly (string Tr, string De, string Priority, string ChangeFreq)[] LocalizedPages =
        {
            ("/",                     "/de",            "1.0", "daily"),
            ("/yazilar",              "/beitraege",     "0.9", "daily"),
            ("/hekim-almancasi",      "/aerztedeutsch", "0.8", "monthly"),
            ("/hemsirelik-almancasi", "/pflegedeutsch", "0.8", "monthly"),
            ("/sinavlar",             "/pruefungen",    "0.8", "weekly"),
            ("/sozluk",               "/woerterbuch",   "0.7", "weekly"),
            ("/hakkimizda",           "/ueber-uns",     "0.6", "monthly"),
            ("/gizlilik",             "/datenschutz",   "0.2", "yearly"),
            ("/kullanim-kosullari",   "/nutzungsbedingungen", "0.2", "yearly"),
        };

        public SitemapController(ILogger<SitemapController> logger, SpeakingClubContext context)
        {
            _context = context;
            _logger = logger;
        }

        // An old address of the sitemap that is still listed in Search Console (it returned 404)
        [HttpGet("Sitemap")]
        public IActionResult LegacySitemap() => RedirectPermanent("/sitemap.xml");

        [HttpGet]
        [Route("sitemap.xml")]
        [ResponseCache(Duration = 3600)] // Cache for 1 hour
        public async Task<IActionResult> Index()
        {
            try
            {
                var urls = new List<XElement>();

                foreach (var page in LocalizedPages)
                {
                    var alternates = new Dictionary<string, string>
                    {
                        ["tr"] = SITE_URL + page.Tr,
                        ["de"] = SITE_URL + page.De
                    };
                    urls.Add(CreateUrl(SITE_URL + page.Tr, null, page.Priority, page.ChangeFreq, alternates));
                    urls.Add(CreateUrl(SITE_URL + page.De, null, page.Priority, page.ChangeFreq, alternates));
                }

                // Only the fields the sitemap needs (no Content).
                var blogs = await _context.Blogs
                    .Where(b => b.IsPublished && !b.NoIndex)
                    .OrderByDescending(b => b.LastModified ?? b.Date)
                    .Select(b => new
                    {
                        b.Slug,
                        b.Url,
                        b.Date,
                        b.LastModified,
                        b.ViewCount,
                        b.isHome,
                        Translations = b.Translations
                            .Select(t => new { t.LanguageCode, t.Slug, t.LastModified })
                            .ToList()
                    })
                    .ToListAsync();

                foreach (var blog in blogs)
                {
                    var tr = blog.Translations.FirstOrDefault(t => t.LanguageCode == "tr");
                    var de = blog.Translations.FirstOrDefault(t => t.LanguageCode == "de");
                    var baseSlug = FirstNonEmpty(blog.Slug, blog.Url);

                    // A language gets a URL only when the post exists in it: a German URL that showed the
                    // Turkish text would be a duplicate. Posts without translations are served from the
                    // base fields under the Turkish URL. Same rules as HomeController.BlogDetail.
                    var trSlug = tr != null ? FirstNonEmpty(tr.Slug, baseSlug) : de == null ? baseSlug : null;
                    var deSlug = de != null ? FirstNonEmpty(de.Slug, baseSlug) : null;

                    var alternates = new Dictionary<string, string>();
                    if (!string.IsNullOrEmpty(trSlug)) alternates["tr"] = $"{SITE_URL}/yazilar/{trSlug}";
                    if (!string.IsNullOrEmpty(deSlug)) alternates["de"] = $"{SITE_URL}/beitraege/{deSlug}";
                    if (alternates.Count == 0) continue;

                    var lastmod = new[] { blog.LastModified, tr?.LastModified, de?.LastModified, blog.Date }
                        .Where(d => d.HasValue)
                        .Max();
                    var changefreq = DetermineChangeFrequency(lastmod ?? blog.Date);
                    var priority = DeterminePriority(blog.Date, blog.ViewCount, blog.isHome);

                    // Hreflang only makes sense when both versions exist.
                    var hreflang = alternates.Count > 1 ? alternates : null;
                    foreach (var loc in alternates.Values)
                    {
                        urls.Add(CreateUrl(loc, lastmod, priority, changefreq, hreflang));
                    }
                }

                _logger.LogInformation("Sitemap generated with {Count} URLs ({Blogs} blog posts)", urls.Count, blogs.Count);

                var sitemap = new XDocument(
                    new XDeclaration("1.0", "utf-8", null),
                    new XProcessingInstruction("xml-stylesheet", "type=\"text/xsl\" href=\"/sitemap.xsl\""),
                    new XElement(Ns + "urlset",
                        new XAttribute(XNamespace.Xmlns + "xhtml", NsXhtml),
                        urls
                    )
                );

                using var ms = new MemoryStream();
                using (var writer = new StreamWriter(ms, new UTF8Encoding(false), leaveOpen: true))
                {
                    sitemap.Save(writer);
                }
                return File(ms.ToArray(), "application/xml");
            }
            catch (Exception ex)
            {
                // A 500 makes Google retry later; a 200 with an error body would be read as an empty sitemap.
                _logger.LogError(ex, "Error generating sitemap");
                return StatusCode(500);
            }
        }

        /// <summary>
        /// Creates a sitemap &lt;url&gt; element. When <paramref name="alternates"/> is given, adds
        /// xhtml:link hreflang entries for every language version plus x-default (the Turkish one).
        /// </summary>
        private static XElement CreateUrl(string loc, DateTime? lastmod, string priority, string changefreq,
            IReadOnlyDictionary<string, string>? alternates = null)
        {
            var url = new XElement(Ns + "url", new XElement(Ns + "loc", loc));

            if (lastmod.HasValue)
            {
                url.Add(new XElement(Ns + "lastmod", lastmod.Value.ToString("yyyy-MM-ddTHH:mm:ss+00:00", CultureInfo.InvariantCulture)));
            }
            url.Add(new XElement(Ns + "changefreq", changefreq));
            url.Add(new XElement(Ns + "priority", priority));

            if (alternates != null)
            {
                foreach (var (lang, href) in alternates)
                {
                    url.Add(HreflangLink(lang, href));
                }
                url.Add(HreflangLink("x-default", alternates.TryGetValue("tr", out var tr) ? tr : alternates.Values.First()));
            }

            return url;
        }

        private static XElement HreflangLink(string hreflang, string href) =>
            new XElement(NsXhtml + "link",
                new XAttribute("rel", "alternate"),
                new XAttribute("hreflang", hreflang),
                new XAttribute("href", href));

        private static string FirstNonEmpty(params string?[] values) =>
            values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v)) ?? string.Empty;

        /// <summary>
        /// Determines change frequency based on how recently the post changed
        /// </summary>
        private static string DetermineChangeFrequency(DateTime lastModified)
        {
            var daysSinceModified = (DateTime.UtcNow - lastModified).TotalDays;
            if (daysSinceModified < 7) return "daily";
            if (daysSinceModified < 90) return "weekly";
            if (daysSinceModified < 365) return "monthly";
            return "yearly";
        }

        /// <summary>
        /// Determines priority based on blog importance (home page feature, popularity, recency)
        /// </summary>
        private static string DeterminePriority(DateTime published, int viewCount, bool isHome)
        {
            var score = 0.5;
            if (isHome) score += 0.2;

            if (viewCount > 1000) score += 0.15;
            else if (viewCount > 500) score += 0.1;
            else if (viewCount > 100) score += 0.05;

            var daysSincePublished = (DateTime.UtcNow - published).TotalDays;
            if (daysSincePublished < 7) score += 0.15;
            else if (daysSincePublished < 30) score += 0.1;
            else if (daysSincePublished < 90) score += 0.05;

            return Math.Min(score, 1.0).ToString("0.0", CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// llms.txt (https://llmstxt.org): a short Markdown guide to the site for AI assistants and agents.
        /// Lighthouse's agentic browsing check wants an H1, enough text and at least one Markdown link.
        /// </summary>
        [HttpGet("/llms.txt")]
        [ResponseCache(Duration = 3600)]
        public async Task<IActionResult> LlmsTxt()
        {
            var sb = new StringBuilder();
            sb.AppendLine("# Almanca Konuş");
            sb.AppendLine();
            sb.AppendLine("> The personal website of Suna Koç, a German teacher who grew up in Bremen (German is her native");
            sb.AppendLine("> language) and teaches Turkish speakers online: one-to-one and small-group (4–6 people) German");
            sb.AppendLine("> conversation lessons, free level tests, a German–Turkish dictionary and clinical German for");
            sb.AppendLine("> doctors and nurses. It is not a company or a school.");
            sb.AppendLine();
            sb.AppendLine("Every page exists in Turkish and German; the Turkish address is listed first. Lessons are arranged");
            sb.AppendLine("personally by e-mail (almancakonus.info@gmail.com) or WhatsApp; the site has no shop or checkout.");
            sb.AppendLine();
            sb.AppendLine("## Pages");
            sb.AppendLine();
            sb.AppendLine(Pair("/", "/de", "Home: Almanca konuşma dersleri / Deutsch sprechen lernen", "lessons, how they work, contact"));
            sb.AppendLine(Pair("/hakkimizda", "/ueber-uns", "About Suna Koç / Über Suna Koç", "her background, teaching style, FAQ and contact form"));
            sb.AppendLine(Pair("/sinavlar", "/pruefungen", "Level tests / Einstufungstests", "free online German tests from A1 to C1 (an account is needed to take them)"));
            sb.AppendLine(Pair("/sozluk", "/woerterbuch", "German–Turkish dictionary / Wörterbuch", "word meanings with examples; search with ?searchTerm=word"));
            sb.AppendLine(Pair("/yazilar", "/beitraege", "Blog / Beiträge", "articles on learning German and living and working in Germany"));
            sb.AppendLine();
            sb.AppendLine("## Clinical German");
            sb.AppendLine();
            sb.AppendLine(Pair("/hekim-almancasi", "/aerztedeutsch", "German for doctors / Ärztedeutsch", "FSP exam, anamnesis, doctor's letters"));
            sb.AppendLine(Pair("/hemsirelik-almancasi", "/pflegedeutsch", "German for nurses / Pflegedeutsch", "handover, care documentation, everyday ward German"));

            var posts = await _context.Blogs
                .Where(b => b.IsPublished && !b.NoIndex)
                .OrderByDescending(b => b.Date)
                .Take(30)
                .Select(b => new
                {
                    b.Title,
                    b.Slug,
                    b.Url,
                    b.MetaDescription,
                    b.Description,
                    Translations = b.Translations
                        .Select(t => new { t.LanguageCode, t.Title, t.Slug, t.MetaDescription, t.Description })
                        .ToList()
                })
                .ToListAsync();

            if (posts.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("## Latest articles");
                sb.AppendLine();
                foreach (var post in posts)
                {
                    // Same URL rules as the sitemap: a language gets a link only when the post exists in it
                    var tr = post.Translations.FirstOrDefault(t => t.LanguageCode == "tr");
                    var de = post.Translations.FirstOrDefault(t => t.LanguageCode == "de");
                    var baseSlug = FirstNonEmpty(post.Slug, post.Url);
                    var trSlug = tr != null ? FirstNonEmpty(tr.Slug, baseSlug) : de == null ? baseSlug : null;
                    var deSlug = de != null ? FirstNonEmpty(de.Slug, baseSlug) : null;

                    if (!string.IsNullOrEmpty(trSlug))
                        sb.AppendLine(Link(FirstNonEmpty(tr?.Title, post.Title), $"{SITE_URL}/yazilar/{trSlug}",
                            FirstNonEmpty(tr?.MetaDescription, tr?.Description, post.MetaDescription, post.Description)));
                    if (!string.IsNullOrEmpty(deSlug))
                        sb.AppendLine(Link(FirstNonEmpty(de?.Title, post.Title), $"{SITE_URL}/beitraege/{deSlug}",
                            FirstNonEmpty(de?.MetaDescription, de?.Description)));
                }
            }

            sb.AppendLine();
            sb.AppendLine("## Optional");
            sb.AppendLine();
            sb.AppendLine(Pair("/gizlilik", "/datenschutz", "Privacy policy / Datenschutz", "what personal data the site processes"));
            sb.AppendLine(Pair("/kullanim-kosullari", "/nutzungsbedingungen", "Terms of use / Nutzungsbedingungen", "rules for accounts, exams and content"));
            sb.AppendLine($"- [Sitemap]({SITE_URL}/sitemap.xml): every public page");

            return Content(sb.ToString(), "text/plain", Encoding.UTF8);  // Markdown, served as text so browsers show it
        }

        private static string Pair(string trPath, string dePath, string title, string notes) =>
            $"- [{title}]({SITE_URL}{trPath}): {notes}. German: {SITE_URL}{dePath}";

        private static string Link(string title, string url, string notes)
        {
            var line = $"- [{EscapeMarkdown(title)}]({url})";
            notes = System.Text.RegularExpressions.Regex.Replace(notes ?? string.Empty, @"<[^>]+>|\s+", " ").Trim();
            if (notes.Length > 160) notes = notes[..157].TrimEnd() + "…";
            return notes.Length > 0 ? $"{line}: {notes}" : line;
        }

        private static string EscapeMarkdown(string text) =>
            text.Replace("[", "\\[").Replace("]", "\\]").Replace("\r", " ").Replace("\n", " ").Trim();

        /// <summary>
        /// robots.txt. Paths are case-sensitive for crawlers, so both spellings of the MVC areas are listed.
        /// </summary>
        [HttpGet("/robots.txt")]
        [ResponseCache(Duration = 86400)] // Cache for 24 hours
        public IActionResult RobotsTxt()
        {
            var sb = new StringBuilder();
            sb.AppendLine("User-agent: *");
            sb.AppendLine("Allow: /");
            sb.AppendLine();
            sb.AppendLine("# Admin and account pages");
            sb.AppendLine("Disallow: /Admin/");
            sb.AppendLine("Disallow: /admin/");
            sb.AppendLine("Disallow: /Account/");
            sb.AppendLine("Disallow: /account/");
            sb.AppendLine("Disallow: /QuizMonitor/");
            sb.AppendLine("Disallow: /quizMonitorHub");
            sb.AppendLine();
            sb.AppendLine("# Endpoints that only redirect or return data");
            sb.AppendLine("Disallow: /Home/SetLanguage");
            sb.AppendLine("Disallow: /download/");
            sb.AppendLine("Disallow: /api/");
            sb.AppendLine();
            sb.AppendLine("# Temporary files");
            sb.AppendLine("Disallow: /temp/");
            sb.AppendLine("Disallow: /uploads/temp/");
            sb.AppendLine();
            sb.AppendLine($"Sitemap: {SITE_URL}/sitemap.xml");

            return Content(sb.ToString(), "text/plain", Encoding.UTF8);
        }
    }
}
