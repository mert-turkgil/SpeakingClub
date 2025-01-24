using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace UI.ViewComponents
{
    public class LanguageSelectorViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke()
        {
            var cultureFeature = HttpContext.Features.Get<IRequestCultureFeature>();
            var currentCulture = cultureFeature?.RequestCulture.Culture;

            // Only include the supported cultures
            var supportedCultures = new List<CultureInfo>
            {
                new CultureInfo("de-DE"),
                new CultureInfo("tr-TR")
            };

            var cultures = supportedCultures.Select(culture => new SelectListItem
            {
                Text = culture.DisplayName,
                Value = culture.Name,
                Selected = culture.Name == currentCulture?.Name
            }).ToList();

            return View(cultures);
        }
    }
}
