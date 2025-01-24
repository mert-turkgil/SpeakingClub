using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.Localization;

namespace UI.Services
{
    public class SharedResource
    {}
    public class LanguageService
    {
        private readonly IStringLocalizer _localizer;

        public LanguageService(IStringLocalizerFactory factory)
        {
            var type = typeof(SharedResource);
            var assemblyFullName = type.Assembly.FullName 
                                   ?? throw new ArgumentNullException(nameof(type.Assembly.FullName), "Assembly FullName cannot be null");
            
            var assemblyName = new AssemblyName(assemblyFullName);
            var location = assemblyName.Name 
                           ?? throw new ArgumentNullException(nameof(assemblyName.Name), "Assembly name cannot be null");

            _localizer = factory.Create(nameof(SharedResource), location)
                        ?? throw new ArgumentNullException(nameof(_localizer), "Localizer cannot be null");
        }
        public LocalizedString Getkey(string key)
        {
            return _localizer[key];
        }
    }
}