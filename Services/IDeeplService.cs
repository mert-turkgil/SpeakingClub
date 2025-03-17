using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SpeakingClub.Services
{
    public interface IDeeplService
    {
        /// <summary>
        /// Retrieves a definition (or translation) for the given term using DeepL API.
        /// Returns null if no definition is found or an error occurs.
        /// </summary>
        Task<string?> GetDefinitionAsync(string term);

        Task<string?> GetDefinitionByCultureAsync(string term ,string culture);
    }
}