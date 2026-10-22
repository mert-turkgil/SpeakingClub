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
            ("/yazilar",              "/beitraege",     "0.9", "daily"),
            ("/hekim-almancasi",      "/aerztedeutsch", "0.8", "monthly"),
            ("/hemsirelik-almancasi", "/pflegedeutsch", "0.8", "monthly"),
            ("/sinavlar",             "/pruefungen",    "0.8", "weekly"),
            ("/sozluk",               "/woerterbuch",   "0.7", "weekly"),
            ("/hakkimizda",           "/ueber-uns",     "0.6", "monthly"),
            ("/gizlilik",             "/datenschutz",   "0.2", "yearly"),
        };

        public SitemapController(ILogger<SitemapController> logger, SpeakingClubContext context)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        [Route("sitemap.xml")]
        [ResponseCache(Duration = 3600)] // Cache for 1 hour
        public async Task<IActionResult> Index()
        {
            try
            {
                var urls = new List<XElement>();

                // The home page has a single URL (its language follows the visitor), so it has no alternates.
                urls.Add(CreateUrl(SITE_URL + "/", null, "1.0", "daily"));

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
