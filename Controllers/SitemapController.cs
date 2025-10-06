using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using SpeakingClub.Data;
using System.Text;

namespace SpeakingClub.Controllers
{
    [Route("[controller]")]
    public class SitemapController : Controller
    {
        private readonly SpeakingClubContext _context;
        private readonly ILogger<SitemapController> _logger;

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
            // TODO: Replace with your actual domain
            var siteUrl = "https://almanca-konus.com"; 
            
            var urls = new List<XElement>();

            // Static pages with priority and change frequency
            urls.Add(CreateUrl(siteUrl + "/", priority: "1.0", changefreq: "daily"));
            urls.Add(CreateUrl(siteUrl + "/about", priority: "0.8", changefreq: "monthly"));
            urls.Add(CreateUrl(siteUrl + "/privacy", priority: "0.3", changefreq: "yearly"));
            urls.Add(CreateUrl(siteUrl + "/words", priority: "0.9", changefreq: "weekly"));

            // ========================================
            // BLOG SECTION - UNCOMMENT WHEN READY
            // ========================================
            /*
            // Blog listing page
            urls.Add(CreateUrl(siteUrl + "/blog", priority: "0.9", changefreq: "daily"));
            
            // Dynamic: Blog posts
            var blogs = await _context.Blogs
                .Include(b => b.Tags)
                .Include(b => b.Category)
                .Where(b => b.isHome == true) // Only published/home blogs
                .OrderByDescending(b => b.Date)
                .ToListAsync();

            foreach (var blog in blogs)
            {
                urls.Add(CreateUrl(
                    $"{siteUrl}/blog/{blog.Url}", 
                    lastmod: blog.Date, 
                    priority: "0.8", 
                    changefreq: "monthly"
                ));
            }
            */
            // ========================================

            // Create XML sitemap
            XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";
            var sitemap = new XDocument(
                new XDeclaration("1.0", "utf-8", "yes"),
                new XElement(ns + "urlset", urls)
            );

            var xml = sitemap.ToString();
            return Content(xml, "application/xml", Encoding.UTF8);
        }

        /// <summary>
        /// Creates a URL element for the sitemap with all SEO attributes
        /// </summary>
        /// <param name="loc">The URL location</param>
        /// <param name="lastmod">Last modification date</param>
        /// <param name="priority">Priority (0.0 to 1.0)</param>
        /// <param name="changefreq">Change frequency: always, hourly, daily, weekly, monthly, yearly, never</param>
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

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View("Error!");
        }
    }
}