using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.Localization;

namespace SpeakingClub.Services
{
    public class SharedResource
    { }

    public class LanguageService
    {
        private readonly IStringLocalizer _localizer;

        public LanguageService(IStringLocalizerFactory factory)
        {
            var type = typeof(SharedResource);

            // Ensure Assembly.FullName is not null
            var assemblyFullName = type.Assembly.FullName 
                ?? throw new InvalidOperationException("Assembly FullName cannot be null.");

            var assemblyName = new AssemblyName(assemblyFullName);

            // Ensure AssemblyName.Name is not null
            var location = assemblyName.Name 
                ?? throw new InvalidOperationException("Assembly name cannot be null.");

            _localizer = factory.Create(nameof(SharedResource), location);
        }

        public LocalizedString GetKey(string key)
        {
            return _localizer[key];
        }
    }
}