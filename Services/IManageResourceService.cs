using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SpeakingClub.Models;

namespace SpeakingClub.Services
{
    public interface IManageResourceService
    {
        /// <summary>
        /// Adds or updates a resource key-value pair in the specified culture's resource file.
        /// </summary>
        /// <param name="key">The resource key.</param>
        /// <param name="value">The resource value.</param>
        /// <param name="culture">The culture name (e.g.,   "fr-FR").</param>
        void AddOrUpdateResource(string key, string value, string culture);

        /// <summary>
        /// Deletes a resource key from the specified culture's resource file.
        /// </summary>
        /// <param name="key">The resource key to delete.</param>
        /// <param name="culture">The culture name (e.g.,   "fr-FR").</param>
        void DeleteResource(string key, string culture);

        /// <summary>
        /// Reads the value of a specific resource key in the specified culture's resource file.
        /// </summary>
        /// <param name="key">The resource key to retrieve.</param>
        /// <param name="culture">The culture name (e.g.,   "fr-FR").</param>
        /// <returns>The resource value if found; otherwise, null.</returns>
        string ReadResourceValue(string key, string culture);
        List<LocalizationModel> LoadAll(string culture);
        bool AddOrUpdate(string key, string value, string culture, string? comment = null);
        bool Delete(string key, string culture);
        string? Read(string key, string culture);
        bool Exists(string key, string culture);
        List<string> GetAvailableLanguages();
        List<string> ExtractImagePaths(string keyPrefix, string culture);
        string GetResourcePath(string culture);
    }
}