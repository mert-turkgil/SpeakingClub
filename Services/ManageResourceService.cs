using System;
using System.Collections.Generic;
using System.Linq;
using System.Resources.NetStandard;
using System.Threading.Tasks;
using System.Xml.Linq;

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
    }
}