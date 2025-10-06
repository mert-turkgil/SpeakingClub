using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Localization;

namespace SpeakingClub.Services
{
    public class SharedResource
    { }

    public class LanguageService
    {
        private readonly IStringLocalizer _localizer;
        private readonly AliveResourceService _aliveResourceService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private static DateTime _lastCacheInvalidation = DateTime.MinValue;

        public LanguageService(IStringLocalizerFactory factory, AliveResourceService aliveResourceService, IHttpContextAccessor httpContextAccessor)
        {
            _aliveResourceService = aliveResourceService;
            _httpContextAccessor = httpContextAccessor;
            
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
            // Get current culture
            var currentCulture = _httpContextAccessor.HttpContext?.Request.HttpContext.Features
                .Get<Microsoft.AspNetCore.Localization.IRequestCultureFeature>()?.RequestCulture.Culture.Name 
                ?? CultureInfo.CurrentCulture.Name;

            // Try to get from AliveResourceService first (this checks file modification times)
            var aliveValue = _aliveResourceService.GetResource(key, currentCulture);
            
            // If AliveResourceService returns the key in brackets, it means not found
            // Fall back to IStringLocalizer
            if (!aliveValue.StartsWith("[") || !aliveValue.EndsWith("]"))
            {
                return new LocalizedString(key, aliveValue, false);
            }

            // Fall back to standard localizer
            return _localizer[key];
        }
        
        public void InvalidateCache()
        {
            _lastCacheInvalidation = DateTime.UtcNow;
        }
    }
}