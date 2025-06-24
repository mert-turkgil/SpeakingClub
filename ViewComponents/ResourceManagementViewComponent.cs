using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Threading.Tasks;
using SpeakingClub.Models;
using SpeakingClub.Services;
using System.Globalization;

public class ResourceManagementViewComponent : ViewComponent
{
    private readonly IWebHostEnvironment _env;

    public ResourceManagementViewComponent(IWebHostEnvironment env)
    {
        _env = env;
    }

    public IViewComponentResult Invoke(string resourceLang)
    {
        // Fall back if nothing is chosen
        if (string.IsNullOrEmpty(resourceLang))
            resourceLang = "tr-TR";

        var translations = LoadTranslations(GetResxPath(resourceLang));

        var viewModel = new LocalizationViewModel
        {
            CurrentLanguage = resourceLang,
            AvailableLanguages = new List<string> { "de-DE",   "tr-TR" },
            Translations = translations
        };

        return View("Default", viewModel);
    }

    private string GetResxPath(string lang)
    {
        var fileName = $"SharedResource.{lang}.resx";
        return Path.Combine(_env.ContentRootPath, "Resources", fileName);
    }

    private List<LocalizationModel> LoadTranslations(string resxPath)
    {
        if (!System.IO.File.Exists(resxPath))
            return new List<LocalizationModel>();

        var resxFile = XDocument.Load(resxPath);
        return resxFile.Root?.Elements("data").Select(x => new LocalizationModel
        {
            Key = x.Attribute("name")?.Value ?? "N/A",
            Value = x.Element("value")?.Value ?? "N/A",
            Comment = x.Element("comment")?.Value ?? "N/A"
        }).ToList() ?? new List<LocalizationModel>();
    }
}
