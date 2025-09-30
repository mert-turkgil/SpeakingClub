using System;
using System.Collections.Generic;
using System.Linq;
using System.Resources.NetStandard;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using SpeakingClub.Models;

namespace SpeakingClub.Services
{
    public class ManageResourceService : IManageResourceService
    {
        private readonly string _resourcePath;

        public ManageResourceService(string resourcePath)
        {
            _resourcePath = resourcePath ?? throw new ArgumentNullException(nameof(resourcePath)); // Null safety
        }

        public void AddOrUpdateResource(string key, string value, string culture)
        {
            if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(value))
                throw new ArgumentNullException("Key or value cannot be null or empty.");

            var filePath = Path.Combine(_resourcePath, $"SharedResource.{culture}.resx");
            var resxEntries = new Dictionary<string, string>();

            // Read existing entries safely
            if (File.Exists(filePath))
            {
                using (var reader = new ResXResourceReader(filePath))
                {
                    foreach (System.Collections.DictionaryEntry entry in reader)
                    {
                        resxEntries[entry.Key?.ToString() ?? ""] = entry.Value?.ToString() ?? string.Empty;
                    }
                }
            }

            // Add or update the key-value pair
            resxEntries[key] = value;

            using (var writer = new ResXResourceWriter(filePath))
            {
                foreach (var entry in resxEntries)
                {
                    writer.AddResource(entry.Key, entry.Value);
                }
            }

            Console.WriteLine($"Resource key '{key}' added/updated with value '{value}' in '{culture}' resource file.");
        }

        public void DeleteResource(string key, string culture)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentNullException(nameof(key));

            var filePath = Path.Combine(_resourcePath, $"SharedResource.{culture}.resx");

            if (!File.Exists(filePath))
            {
                Console.WriteLine($"Resource file '{filePath}' not found.");
                return;
            }

            var resxEntries = new Dictionary<string, string>();

            // Read existing entries safely
            using (var reader = new ResXResourceReader(filePath))
            {
                foreach (System.Collections.DictionaryEntry entry in reader)
                {
                    if (entry.Key?.ToString() != key) // Null check
                    {
                        resxEntries[entry.Key?.ToString() ?? ""] = entry.Value?.ToString() ?? string.Empty;
                    }
                }
            }

            using (var writer = new ResXResourceWriter(filePath))
            {
                foreach (var entry in resxEntries)
                {
                    writer.AddResource(entry.Key, entry.Value);
                }
            }

            Console.WriteLine($"Resource key '{key}' deleted from '{culture}' resource file.");
        }

        public string ReadResourceValue(string key, string culture)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentNullException(nameof(key));

            var filePath = Path.Combine(_resourcePath, $"SharedResource.{culture}.resx");

            if (!File.Exists(filePath))
            {
                Console.WriteLine($"Resource file '{filePath}' not found.");
                return string.Empty; // Ensure non-null return
            }

            var xDocument = XDocument.Load(filePath);
            var dataElement = xDocument.Root?.Elements("data")
                .FirstOrDefault(x => x.Attribute("name")?.Value == key);

            return dataElement?.Element("value")?.Value ?? string.Empty; // Default to empty string
        }
        public List<LocalizationModel> LoadAll(string culture)
        {
            var resxPath = GetResourcePath(culture);
            if (!File.Exists(resxPath))
                return new List<LocalizationModel>();

            var doc = XDocument.Load(resxPath);
            return doc.Root?
                .Elements("data")
                .Select(x => new LocalizationModel
                {
                    Key = x.Attribute("name")?.Value ?? "",
                    Value = x.Element("value")?.Value ?? "",
                    Comment = x.Element("comment")?.Value ?? ""
                })
                .ToList() ?? new List<LocalizationModel>();
        }

        public bool AddOrUpdate(string key, string value, string culture, string? comment = null)
        {
            var resxPath = GetResourcePath(culture);
            if (!File.Exists(resxPath))
                return false;

            var doc = XDocument.Load(resxPath);
            var data = doc.Root?.Elements("data").FirstOrDefault(x => x.Attribute("name")?.Value == key);

            if (data == null)
            {
                data = new XElement("data",
                            new XAttribute("name", key),
                            new XAttribute(XNamespace.Xml + "space", "preserve"),
                            new XElement("value", value ?? ""),
                            new XElement("comment", comment ?? "")
                        );
                doc.Root?.Add(data);
            }
            else
            {
                data.SetElementValue("value", value ?? "");
                data.SetElementValue("comment", comment ?? "");
            }

            doc.Save(resxPath);
            return true;
        }

        public bool Delete(string key, string culture)
        {
            var resxPath = GetResourcePath(culture);
            if (!File.Exists(resxPath))
                return false;

            var doc = XDocument.Load(resxPath);
            var data = doc.Root?.Elements("data").FirstOrDefault(x => x.Attribute("name")?.Value == key);

            if (data == null)
                return false;

            data.Remove();
            doc.Save(resxPath);
            return true;
        }

        public string? Read(string key, string culture)
        {
            var resxPath = GetResourcePath(culture);
            if (!File.Exists(resxPath))
                return null;

            var doc = XDocument.Load(resxPath);
            var value = doc.Root?
                .Elements("data")
                .FirstOrDefault(x => x.Attribute("name")?.Value == key)?
                .Element("value")?.Value;

            return value;
        }

        public bool Exists(string key, string culture)
        {
            var resxPath = GetResourcePath(culture);
            if (!File.Exists(resxPath))
                return false;

            var doc = XDocument.Load(resxPath);
            return doc.Root?
                .Elements("data")
                .Any(x => x.Attribute("name")?.Value == key) ?? false;
        }

        public List<string> GetAvailableLanguages()
        {
            var resxFiles = Directory
                .GetFiles(_resourcePath, "SharedResource.*.resx")
                .Select(Path.GetFileName)
                .Where(f => !string.IsNullOrWhiteSpace(f)) // Ensure not null or empty
                .Select(f => Path.GetFileNameWithoutExtension(f))
                .Where(name => !string.IsNullOrWhiteSpace(name) && name!.StartsWith("SharedResource."))
                .Select(name => name!.Replace("SharedResource.", ""))
                .ToList();

            return resxFiles;
        }


        public List<string> ExtractImagePaths(string keyPrefix, string culture)
        {
            List<string> imagePaths = new();
            var resxPath = GetResourcePath(culture);
            if (!File.Exists(resxPath))
                return imagePaths;

            var doc = XDocument.Load(resxPath);
            var dataElements = doc.Root?
                .Elements("data")
                .Where(x => x.Attribute("name")?.Value.StartsWith(keyPrefix) == true);

            if (dataElements != null)
            {
                foreach (var element in dataElements)
                {
                    var content = element.Element("value")?.Value ?? "";
                    var matches = Regex.Matches(content, @"src=[""'](?<url>/blog/(img|gif)/.*?\.(jpg|jpeg|png|gif))[""']");
                    foreach (Match match in matches)
                    {
                        imagePaths.Add(match.Groups["url"].Value.TrimStart('/'));
                    }
                }
            }

            return imagePaths;
        }

        public string GetResourcePath(string culture)
        {
            var fileName = $"SharedResource.{culture}.resx";
            return Path.Combine(_resourcePath, fileName);
        }
    }
}