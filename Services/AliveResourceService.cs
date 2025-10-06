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
        private readonly ConcurrentDictionary<string, (DateTime LoadTime, XDocument Document)> _resourceCache = new();
        private readonly string _resourcePath;
        private static DateTime _lastReloadTime = DateTime.MinValue;

        public AliveResourceService(string resourcePath)
        {
            _resourcePath = resourcePath;
        }

        public string GetResource(string key, string culture)
        {
            var resxPath = Path.Combine(_resourcePath, $"SharedResource.{culture}.resx");
            
            if (!File.Exists(resxPath))
            {
                return $"[{key}]"; // Default value if file not found
            }

            // Check if we need to reload based on file modification time or manual reload
            var fileInfo = new FileInfo(resxPath);
            var cacheKey = $"{culture}";

            XDocument xDocument;
            
            if (_resourceCache.TryGetValue(cacheKey, out var cached))
            {
                // Check if file was modified after cache or if manual reload was triggered
                if (fileInfo.LastWriteTimeUtc > cached.LoadTime || _lastReloadTime > cached.LoadTime)
                {
                    // File was modified or manual reload triggered, reload it
                    xDocument = XDocument.Load(resxPath);
                    _resourceCache[cacheKey] = (DateTime.UtcNow, xDocument);
                }
                else
                {
                    // Use cached version
                    xDocument = cached.Document;
                }
            }
            else
            {
                // First time loading this resource file
                xDocument = XDocument.Load(resxPath);
                _resourceCache[cacheKey] = (DateTime.UtcNow, xDocument);
            }

            var dataElement = xDocument.Root?.Elements("data")
                .FirstOrDefault(x => x.Attribute("name")?.Value == key);

            return dataElement?.Element("value")?.Value ?? $"[{key}]";
        }

        public void ReloadResources()
        {
            // Clear all cached resources and update reload timestamp
            _resourceManagers.Clear();
            _resourceCache.Clear();
            _lastReloadTime = DateTime.UtcNow;
            
            // Force garbage collection to ensure old resources are released
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
    }
}