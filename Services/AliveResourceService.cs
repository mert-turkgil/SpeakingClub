using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Resources;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace SpeakingClub.Services
{
    public class AliveResourceService
    {
        private readonly ConcurrentDictionary<string, ResourceManager> _resourceManagers = new();
        private readonly string _resourcePath;

        public AliveResourceService(string resourcePath)
        {
            _resourcePath = resourcePath;
        }

        public string GetResource(string key, string culture)
        {
            var resxPath = Path.Combine(_resourcePath, $"SharedResource.{culture}.resx");
            if (File.Exists(resxPath))
            {
                var xDocument = XDocument.Load(resxPath);
                var dataElement = xDocument.Root?.Elements("data")
                    .FirstOrDefault(x => x.Attribute("name")?.Value == key);

                return dataElement?.Element("value")?.Value ?? $"[{key}]";
            }

            return $"[{key}]"; // Default value if key not found
        }

        public void ReloadResources()
        {
            _resourceManagers.Clear(); // Clear resource cache
        }
    }
}