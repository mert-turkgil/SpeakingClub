using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SpeakingClub.Services
{
    public interface IManageResourceService
    {
                /// <summary>
        /// Adds or updates a resource key-value pair in the specified culture's resource file.
        /// </summary>
        /// <param name="key">The resource key.</param>
        /// <param name="value">The resource value.</param>
        /// <param name="culture">The culture name (e.g., "en-US", "fr-FR").</param>
        void AddOrUpdateResource(string key, string value, string culture);

        /// <summary>
        /// Deletes a resource key from the specified culture's resource file.
        /// </summary>
        /// <param name="key">The resource key to delete.</param>
        /// <param name="culture">The culture name (e.g., "en-US", "fr-FR").</param>
        void DeleteResource(string key, string culture);

        /// <summary>
        /// Reads the value of a specific resource key in the specified culture's resource file.
        /// </summary>
        /// <param name="key">The resource key to retrieve.</param>
        /// <param name="culture">The culture name (e.g., "en-US", "fr-FR").</param>
        /// <returns>The resource value if found; otherwise, null.</returns>
        string ReadResourceValue(string key, string culture);
    }
}