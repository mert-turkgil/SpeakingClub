using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using SpeakingClub.Models;

// Admin → Site metinleri: every text of Resources/SharedResource.<lang>.resx, Turkish and German side by
// side (the old list showed one language at a time behind a dropdown of culture codes). Texts that are
// edited elsewhere — slide captions (Carousel_*) and the old per-post blog keys — are left out.
public class ResourceManagementViewComponent : ViewComponent
{
    private static readonly Regex ManagedElsewhere = new(@"^(Carousel_\d+_|(Title|Content)_\d+_)", RegexOptions.Compiled);
    private readonly IWebHostEnvironment _env;

    public ResourceManagementViewComponent(IWebHostEnvironment env)
    {
        _env = env;
    }

    public IViewComponentResult Invoke()
    {
        var tr = Load("tr-TR");
        var de = Load("de-DE");
        var keys = tr.Select(e => e.Key).Concat(de.Select(e => e.Key)).Distinct()
            .Where(k => !ManagedElsewhere.IsMatch(k))
            .ToList();
        var trByKey = tr.GroupBy(e => e.Key).ToDictionary(g => g.Key, g => g.First());
        var deByKey = de.GroupBy(e => e.Key).ToDictionary(g => g.Key, g => g.First());
        var rows = keys.Select(k => new SiteTextRow(
            k,
            trByKey.TryGetValue(k, out var t) ? t.Value : string.Empty,
            deByKey.TryGetValue(k, out var d) ? d.Value : string.Empty,
            t?.Comment ?? d?.Comment)).ToList();
        return View("Default", rows);
    }

    private List<LocalizationModel> Load(string lang)
    {
        var path = Path.Combine(_env.ContentRootPath, "Resources", $"SharedResource.{lang}.resx");
        if (!System.IO.File.Exists(path))
            return new List<LocalizationModel>();

        return XDocument.Load(path).Root?.Elements("data").Select(x => new LocalizationModel
        {
            Key = x.Attribute("name")?.Value ?? string.Empty,
            Value = x.Element("value")?.Value ?? string.Empty,
            // An empty comment used to come back as "N/A" and was saved into the file on the next edit
            Comment = x.Element("comment")?.Value ?? string.Empty
        }).Where(m => m.Key.Length > 0).ToList() ?? new List<LocalizationModel>();
    }
}
