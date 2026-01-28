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

                // Static pages with priority and change frequency
                urls.Add(CreateUrl(SITE_URL + "/", priority: "1.0", changefreq: "daily"));
                urls.Add(CreateUrl(SITE_URL + "/about", priority: "0.8", changefreq: "monthly"));
                urls.Add(CreateUrl(SITE_URL + "/privacy", priority: "0.3", changefreq: "yearly"));
                urls.Add(CreateUrl(SITE_URL + "/words", priority: "0.9", changefreq: "weekly"));
                urls.Add(CreateUrl(SITE_URL + "/blog", priority: "0.9", changefreq: "daily"));

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
                    // Use Slug instead of Url for SEO-friendly URLs
                    var blogUrl = $"{SITE_URL}/blog/{blog.Slug}";
                    
                    // Determine change frequency based on view count and recency
                    var changefreq = DetermineChangeFrequency(blog);
                    
                    // Determine priority based on isHome, ViewCount, and recency
                    var priority = DeterminePriority(blog);
                    
                    // Create main URL element with alternate language links
                    var urlElement = CreateUrlWithAlternates(
                        blogUrl,
                        lastmod: blog.LastModified ?? blog.Date,
                        priority: priority,
                        changefreq: changefreq
                    );
                    var nsXhtml = XNamespace.Get("http://www.w3.org/1999/xhtml");
                    
                    // Add English (default)
                    urlElement.Add(new XElement(nsXhtml + "link",
                        new XAttribute("rel", "alternate"),
                        new XAttribute("hreflang", "en"),
                        new XAttribute("href", blogUrl)
                    ));
                    
                    // Add Turkish translation if exists
                    var trTranslation = blog.Translations?.FirstOrDefault(t => t.LanguageCode == "tr");
                    if (trTranslation != null && !string.IsNullOrEmpty(trTranslation.Slug))
                    {
                        urlElement.Add(new XElement(nsXhtml + "link",
                            new XAttribute("rel", "alternate"),
                            new XAttribute("hreflang", "tr"),
                            new XAttribute("href", $"{SITE_URL}/tr/blog/{trTranslation.Slug}")
                        ));
                    }
                    
                    // Add German translation if exists
                    var deTranslation = blog.Translations?.FirstOrDefault(t => t.LanguageCode == "de");
                    if (deTranslation != null && !string.IsNullOrEmpty(deTranslation.Slug))
                    {
                        urlElement.Add(new XElement(nsXhtml + "link",
                            new XAttribute("rel", "alternate"),
                            new XAttribute("hreflang", "de"),
                            new XAttribute("href", $"{SITE_URL}/de/blog/{deTranslation.Slug}")
                        ));
                    }
                    
                    // Add x-default for unmatched languages (points to English version)
                    urlElement.Add(new XElement(nsXhtml + "link",
                        new XAttribute("rel", "alternate"),
                        new XAttribute("hreflang", "x-default"),
                        new XAttribute("href", blogUrl)
                    ));
                    
                    urls.Add(urlElement);
                }

                // Create XML sitemap with namespaces
                var ns = XNamespace.Get("http://www.sitemaps.org/schemas/sitemap/0.9");

                var nsXhtmll = XNamespace.Get("http://www.w3.org/1999/xhtml");
                var sitemap = new XDocument(
                    new XDeclaration("1.0", "utf-8", "yes"),
                    new XElement(ns + "urlset",
                        new XAttribute(XNamespace.Xmlns + "xhtml", nsXhtmll),
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