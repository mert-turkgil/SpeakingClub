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
        public async Task<IActionResult> Index()
        {
            var siteUrl = "https://almanca-konus.com"; // kendi domainini yaz

            var urls = new List<XElement>
            {
                // Sabit sayfalar
                CreateUrl(siteUrl + "/"),
                CreateUrl(siteUrl + "/about"),
                CreateUrl(siteUrl + "/blog"),
                CreateUrl(siteUrl + "/words")
            };

            // Dinamik: Bloglar
            var blogs = await _context.Blogs
                .ToListAsync();

            foreach (var blog in blogs)
            {
                urls.Add(CreateUrl($"{siteUrl}/blog/{blog.Url}", blog.Date));
            }

            // Dinamik: Sözlük (Words)
            var words = await _context.Words.ToListAsync();
            foreach (var word in words)
            {
                urls.Add(CreateUrl($"{siteUrl}/words"));
            }

            // Dinamik: Quizler
            var quizzes = await _context.Quizzes.ToListAsync();
            foreach (var quiz in quizzes)
            {
                urls.Add(CreateUrl($"{siteUrl}/Quiz/Detail/{quiz.Id}"));
            }

            // XML oluştur
            XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";
            var sitemap = new XDocument(
                new XDeclaration("1.0", "utf-8", "yes"),
                new XElement(ns + "urlset", urls)
            );

            // Response ayarla
            var xml = sitemap.ToString();
            return Content(xml, "application/xml", Encoding.UTF8);
        }

        private XElement CreateUrl(string loc, DateTime? lastmod = null)
        {
            XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";
            var url = new XElement(ns + "url",
                new XElement(ns + "loc", loc)
            );
            if (lastmod.HasValue)
                url.Add(new XElement(ns + "lastmod", lastmod.Value.ToString("yyyy-MM-dd")));
            return url;
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View("Error!");
        }
    }
}