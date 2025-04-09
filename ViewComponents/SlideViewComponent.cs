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
            var slideItems = new SlideItemModel{SlideItems = slides.Select(s =>
            {
                var action = string.IsNullOrEmpty(s.CarouselLink) ? "Index" : s.CarouselLink;
                var finalAction = ActionExists(action, "Home") ? action : "Index";
                return new SlideModel
                {
                    SlideId = s.SlideId,
                    CarouselTitle = _localization.GetKey($"Carousel_{s.SlideId}_Title")?.Value ?? s.CarouselTitle,
                    CarouselImage = s.CarouselImage,
                    CarouselImage600w = s.CarouselImage600w,
                    CarouselImage1200w = s.CarouselImage1200w,
                    CarouselDescription =  _localization.GetKey($"Carousel_{s.SlideId}_Description")?.Value ?? s.CarouselDescription,
                    CarouselLink = Url.Action(finalAction, "Home")?? "/", 
                    CarouselLinkText = _localization.GetKey($"Carousel_{s.SlideId}_LinkText")?.Value ?? s.CarouselLinkText,
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