using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace SpeakingClub.ViewComponents
{
    public class LanguageSelectorViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke()
        {
            var cultureFeature = HttpContext.Features.Get<IRequestCultureFeature>();

            // Null check for cultureFeature
            if (cultureFeature?.RequestCulture?.Culture == null)
            {
                throw new InvalidOperationException("RequestCulture feature is not available.");
            }

            var currentCulture = cultureFeature.RequestCulture.Culture;

            // Define supported cultures: Turkish and German only
            var supportedCultureCodes = new[] { "tr-TR", "de-DE" };
            
            var cultures = new List<SelectListItem>();

            foreach (var cultureCode in supportedCultureCodes)
            {
                var culture = new CultureInfo(cultureCode);
                cultures.Add(new SelectListItem
                {
                    Text = culture.NativeName, // Display name in native language (Türkçe, Deutsch)
                    Value = culture.Name,
                    Selected = culture.Name == currentCulture.Name
                });
            }

            return View(cultures);
        }
    }
}