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

            var cultures = new List<SelectListItem>();

            foreach (var culture in CultureInfo.GetCultures(CultureTypes.AllCultures))
            {
                cultures.Add(new SelectListItem
                {
                    Text = culture.DisplayName,
                    Value = culture.Name,
                    Selected = culture.Name == currentCulture.Name
                });
            }

            return View(cultures);
        }
    }
}