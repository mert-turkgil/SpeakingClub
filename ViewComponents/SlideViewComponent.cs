using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SpeakingClub.Controllers;
using SpeakingClub.Data.Abstract;
using SpeakingClub.Models;
using SpeakingClub.Services;

namespace SpeakingClub.ViewComponents
{
    public class SlideViewComponent : ViewComponent
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly LanguageService _localization;
        public SlideViewComponent(IUnitOfWork unitOfWork, LanguageService localization)
        {
            _unitOfWork = unitOfWork;
            _localization = localization;
        }
        public async Task<IViewComponentResult> InvokeAsync()
        {
            var slides = await _unitOfWork.Slides.GetAllAsync(); 
            if (!slides.Any()) {
                return Content(string.Empty);
            }
            var isGerman = System.Globalization.CultureInfo.CurrentCulture.TwoLetterISOLanguageName == "de";
            // Url.Action picks any of an action's routes (/blog, /yazilar or /beitraege), which could send a Turkish
            // visitor to the German page; link to the page in the visitor's language instead.
            string LinkFor(string action) => action.ToLowerInvariant() switch
            {
                "index" => "/",
                "blog" => isGerman ? "/beitraege" : "/yazilar",
                "about" => isGerman ? "/ueber-uns" : "/hakkimizda",
                "words" => isGerman ? "/woerterbuch" : "/sozluk",
                "privacy" => isGerman ? "/datenschutz" : "/gizlilik",
                "quizzes" => isGerman ? "/pruefungen" : "/sinavlar",
                "doctorsgerman" => isGerman ? "/aerztedeutsch" : "/hekim-almancasi",
                "nursesgerman" => isGerman ? "/pflegedeutsch" : "/hemsirelik-almancasi",
                _ => Url.Action(action, "Home") ?? "/"
            };
            // A missing resource comes back as its own key ("Carousel_5_Title"), not null, so "?? fallback" never
            // applied and a slide without translations showed the key on the home page.
            string Text(string key, string? fallback)
            {
                var value = _localization.GetKey(key);
                return value == null || value.ResourceNotFound || string.IsNullOrWhiteSpace(value.Value) ? fallback ?? string.Empty : value.Value;
            }
            var slideItems = new SlideItemModel{SlideItems = slides.Select(s =>
            {
                var action = string.IsNullOrEmpty(s.CarouselLink) ? "Index" : s.CarouselLink;
                var finalAction = ActionExists(action, "Home") ? action : "Index";
                return new SlideModel
                {
                    SlideId = s.SlideId,
                    CarouselTitle = Text($"Carousel_{s.SlideId}_Title", s.CarouselTitle),
                    CarouselImage = s.CarouselImage,
                    CarouselImage600w = s.CarouselImage600w,
                    CarouselImage1200w = s.CarouselImage1200w,
                    CarouselDescription = Text($"Carousel_{s.SlideId}_Description", s.CarouselDescription),
                    CarouselLink = LinkFor(finalAction),
                    CarouselLinkText = Text($"Carousel_{s.SlideId}_LinkText", s.CarouselLinkText),
                    DateAdded = s.DateAdded
                };
            }).ToList()};
            return View(slideItems);
        }
        private bool ActionExists(string actionName, string controllerName)
        {
            var controllerType = typeof(HomeController); // Update this if checking another controller.
            var methods = controllerType.GetMethods();

            return methods.Any(m => 
                m.Name.Equals(actionName, StringComparison.OrdinalIgnoreCase) &&
                m.GetCustomAttributes(typeof(NonActionAttribute), false).Length == 0);
        }
    }
}