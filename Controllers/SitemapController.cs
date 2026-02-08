// ============================================================================
// UPDATED SITEMAP CONTROLLER - Replace your existing SitemapController.cs
// ============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SpeakingClub.Data;

namespace SpeakingClub.Controllers
{
    [Route("[controller]")]
    public class SitemapController : Controller
    {
        private readonly SpeakingClubContext _context;
        private readonly ILogger<SitemapController> _logger;
        private const string SITE_URL = "https://almanca-konus.com"; // Update with your domain

        public SitemapController(ILogger<SitemapController> logger, SpeakingClubContext context)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        [Route("")]
        [Route("sitemap.xml")]
        [ResponseCache(Duration = 3600)] // Cache for 1 hour
        public async Task<IActionResult> Index()
        {
            try
            {
                var urls = new List<XElement>();
                var nsXhtml = XNamespace.Get("http://www.w3.org/1999/xhtml");

                // Static pages with localized alternates (Turkish versions)
                urls.Add(CreateLocalizedUrl(SITE_URL + "/", SITE_URL + "/", SITE_URL + "/", "1.0", "daily"));
                urls.Add(CreateLocalizedUrl(SITE_URL + "/hakkimizda", SITE_URL + "/hakkimizda", SITE_URL + "/ueber-uns", "0.8", "monthly"));
                urls.Add(CreateLocalizedUrl(SITE_URL + "/gizlilik", SITE_URL + "/gizlilik", SITE_URL + "/datenschutz", "0.3", "yearly"));
                urls.Add(CreateLocalizedUrl(SITE_URL + "/sozluk", SITE_URL + "/sozluk", SITE_URL + "/woerterbuch", "0.9", "weekly"));
                urls.Add(CreateLocalizedUrl(SITE_URL + "/yazilar", SITE_URL + "/yazilar", SITE_URL + "/beitraege", "0.9", "daily"));
                urls.Add(CreateLocalizedUrl(SITE_URL + "/sinavlar", SITE_URL + "/sinavlar", SITE_URL + "/pruefungen", "0.9", "weekly"));

                // Static pages with localized alternates (German versions - reciprocal hreflang)
                urls.Add(CreateLocalizedUrl(SITE_URL + "/ueber-uns", SITE_URL + "/hakkimizda", SITE_URL + "/ueber-uns", "0.8", "monthly"));
                urls.Add(CreateLocalizedUrl(SITE_URL + "/datenschutz", SITE_URL + "/gizlilik", SITE_URL + "/datenschutz", "0.3", "yearly"));
                urls.Add(CreateLocalizedUrl(SITE_URL + "/woerterbuch", SITE_URL + "/sozluk", SITE_URL + "/woerterbuch", "0.9", "weekly"));
                urls.Add(CreateLocalizedUrl(SITE_URL + "/beitraege", SITE_URL + "/yazilar", SITE_URL + "/beitraege", "0.9", "daily"));
                urls.Add(CreateLocalizedUrl(SITE_URL + "/pruefungen", SITE_URL + "/sinavlar", SITE_URL + "/pruefungen", "0.9", "weekly"));

                // Dynamic: Blog posts with multilingual support
                var blogs = await _context.Blogs
                    .Include(b => b.Tags)
                    .Include(b => b.Category)
                    .Include(b => b.Translations) // Include translations
                    .Where(b => b.IsPublished && !b.NoIndex) // Only published and not no-indexed
                    .OrderByDescending(b => b.LastModified ?? b.Date)
                    .ToListAsync();

                foreach (var blog in blogs)
                {
                    // Determine change frequency based on view count and recency
                    var changefreq = DetermineChangeFrequency(blog);
                    
                    // Determine priority based on isHome, ViewCount, and recency
                    var priority = DeterminePriority(blog);
                    
                    // Get translations for hreflang
                    var trTranslation = blog.Translations?.FirstOrDefault(t => t.LanguageCode == "tr");
                    var deTranslation = blog.Translations?.FirstOrDefault(t => t.LanguageCode == "de");
                    var trSlug = trTranslation?.Slug ?? blog.Slug;
                    var deSlug = deTranslation?.Slug ?? blog.Slug;
                    
                    // Use Turkish slug as canonical loc (yazilar is the Turkish path)
                    var blogUrl = $"{SITE_URL}/yazilar/{trSlug}";
                    
                    // Create URL with localized alternates
                    var urlElement = CreateUrlWithAlternates(
                        blogUrl,
                        lastmod: blog.LastModified ?? blog.Date,
                        priority: priority,
                        changefreq: changefreq
                    );
                    
                    // Add Turkish version
                    urlElement.Add(new XElement(nsXhtml + "link",
                        new XAttribute("rel", "alternate"),
                        new XAttribute("hreflang", "tr"),
                        new XAttribute("href", $"{SITE_URL}/yazilar/{trSlug}")
                    ));
                    
                    // Add German version
                    urlElement.Add(new XElement(nsXhtml + "link",
                        new XAttribute("rel", "alternate"),
                        new XAttribute("hreflang", "de"),
                        new XAttribute("href", $"{SITE_URL}/beitraege/{deSlug}")
                    ));
                    
                    // Add x-default
                    urlElement.Add(new XElement(nsXhtml + "link",
                        new XAttribute("rel", "alternate"),
                        new XAttribute("hreflang", "x-default"),
                        new XAttribute("href", blogUrl)
                    ));
                    
                    urls.Add(urlElement);
                    
                    // Also add German URL entry with reciprocal hreflang alternates
                    var blogUrlDe = $"{SITE_URL}/beitraege/{deSlug}";
                    var urlElementDe = CreateUrlWithAlternates(
                        blogUrlDe,
                        lastmod: blog.LastModified ?? blog.Date,
                        priority: priority,
                        changefreq: changefreq
                    );
                    
                    urlElementDe.Add(new XElement(nsXhtml + "link",
                        new XAttribute("rel", "alternate"),
                        new XAttribute("hreflang", "tr"),
                        new XAttribute("href", $"{SITE_URL}/yazilar/{trSlug}")
                    ));
                    urlElementDe.Add(new XElement(nsXhtml + "link",
                        new XAttribute("rel", "alternate"),
                        new XAttribute("hreflang", "de"),
                        new XAttribute("href", blogUrlDe)
                    ));
                    urlElementDe.Add(new XElement(nsXhtml + "link",
                        new XAttribute("rel", "alternate"),
                        new XAttribute("hreflang", "x-default"),
                        new XAttribute("href", blogUrl)
                    ));
                    
                    urls.Add(urlElementDe);
                }

                // NOTE: Quiz listing pages (/sinavlar, /pruefungen) are already included 
                // in the static pages section above. No need for per-quiz entries since 
                // quizzes don't have individual detail pages.

                // Create XML sitemap with namespaces
                var ns = XNamespace.Get("http://www.sitemaps.org/schemas/sitemap/0.9");

                var sitemap = new XDocument(
                    new XDeclaration("1.0", "utf-8", "yes"),
                    new XElement(ns + "urlset",
                        new XAttribute(XNamespace.Xmlns + "xhtml", nsXhtml),
                        urls
                    )
                );

                var xml = sitemap.ToString();
                return Content(xml, "application/xml", Encoding.UTF8);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating sitemap");
                return StatusCode(500, "Error generating sitemap");
            }
        }

        /// <summary>
        /// Creates a URL element for the sitemap with all SEO attributes
        /// </summary>
        private XElement CreateUrl(string loc, DateTime? lastmod = null, string priority = "0.5", string changefreq = "monthly")
        {
            XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";
            
            var url = new XElement(ns + "url",
                new XElement(ns + "loc", loc),
                new XElement(ns + "changefreq", changefreq),
                new XElement(ns + "priority", priority)
            );
            
            if (lastmod.HasValue)
            {
                url.Add(new XElement(ns + "lastmod", lastmod.Value.ToString("yyyy-MM-ddTHH:mm:ss+00:00")));
            }
            
            return url;
        }

        /// <summary>
        /// Creates a URL element with localized hreflang alternates for static pages
        /// </summary>
        private XElement CreateLocalizedUrl(string loc, string trHref, string deHref, string priority = "0.5", string changefreq = "monthly")
        {
            XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";
            XNamespace nsXhtml = "http://www.w3.org/1999/xhtml";

            var url = new XElement(ns + "url",
                new XElement(ns + "loc", loc),
                new XElement(ns + "changefreq", changefreq),
                new XElement(ns + "priority", priority)
            );
            
            url.Add(new XElement(nsXhtml + "link",
                new XAttribute("rel", "alternate"),
                new XAttribute("hreflang", "tr"),
                new XAttribute("href", trHref)
            ));
            url.Add(new XElement(nsXhtml + "link",
                new XAttribute("rel", "alternate"),
                new XAttribute("hreflang", "de"),
                new XAttribute("href", deHref)
            ));
            url.Add(new XElement(nsXhtml + "link",
                new XAttribute("rel", "alternate"),
                new XAttribute("hreflang", "x-default"),
                new XAttribute("href", loc)
            ));

            return url;
        }

        /// <summary>
        /// Creates a URL element with support for alternate language links
        /// </summary>
        private XElement CreateUrlWithAlternates(string loc, DateTime? lastmod = null, string priority = "0.5", string changefreq = "monthly")
        {
            XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";
            
            var url = new XElement(ns + "url",
                new XElement(ns + "loc", loc),
                new XElement(ns + "changefreq", changefreq),
                new XElement(ns + "priority", priority)
            );
            
            if (lastmod.HasValue)
            {
                url.Add(new XElement(ns + "lastmod", lastmod.Value.ToString("yyyy-MM-ddTHH:mm:ss+00:00")));
            }
            
            return url;
        }

        /// <summary>
        /// Determines change frequency based on blog age and activity
        /// </summary>
        private string DetermineChangeFrequency(SpeakingClub.Entity.Blog blog)
        {
            var lastModified = blog.LastModified ?? blog.Date;
            var daysSinceModified = (DateTime.UtcNow - lastModified).TotalDays;
            
            // Very recent posts change frequently
            if (daysSinceModified < 7)
                return "daily";
            
            // Recent posts with high engagement
            if (daysSinceModified < 30 && blog.ViewCount > 100)
                return "weekly";
            
            // Active posts
            if (daysSinceModified < 90)
                return "weekly";
            
            // Older posts
            if (daysSinceModified < 365)
                return "monthly";
            
            // Very old posts
            return "yearly";
        }

        /// <summary>
        /// Determines priority based on blog importance
        /// </summary>
        private string DeterminePriority(SpeakingClub.Entity.Blog blog)
        {
            var score = 0.5; // Base priority
            
            // Home page posts get higher priority
            if (blog.isHome)
                score += 0.2;
            
            // Popular posts (high view count) get higher priority
            if (blog.ViewCount > 1000)
                score += 0.15;
            else if (blog.ViewCount > 500)
                score += 0.1;
            else if (blog.ViewCount > 100)
                score += 0.05;
            
            // Recent posts get higher priority
            var daysSincePublished = (DateTime.UtcNow - blog.Date).TotalDays;
            if (daysSincePublished < 7)
                score += 0.15;
            else if (daysSincePublished < 30)
                score += 0.1;
            else if (daysSincePublished < 90)
                score += 0.05;
            
            // Cap at 1.0
            score = Math.Min(score, 1.0);
            
            return score.ToString("0.0");
        }

        /// <summary>
        /// Generates robots.txt content
        /// </summary>
        [HttpGet("/robots.txt")]
        [ResponseCache(Duration = 86400)] // Cache for 24 hours
        public IActionResult RobotsTxt()
        {
            var sb = new StringBuilder();
            sb.AppendLine("User-agent: *");
            sb.AppendLine("Allow: /");
            sb.AppendLine("");
            sb.AppendLine("# Sitemaps");
            sb.AppendLine($"Sitemap: {SITE_URL}/sitemap.xml");
            sb.AppendLine("");
            sb.AppendLine("# Block admin areas");
            sb.AppendLine("Disallow: /admin/");
            sb.AppendLine("Disallow: /account/");
            sb.AppendLine("Disallow: /api/");
            sb.AppendLine("");
            sb.AppendLine("# Block temp files");
            sb.AppendLine("Disallow: /temp/");
            sb.AppendLine("");
            sb.AppendLine("# Crawl delay");
            sb.AppendLine("Crawl-delay: 10");

            return Content(sb.ToString(), "text/plain", Encoding.UTF8);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View("Error!");
        }
    }
}