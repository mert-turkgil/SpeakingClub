using UI.Services;
using UI.Data.Abstract; 
using UI.Entity;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UI.Models;
using UI.Controllers;
using Data.Abstract;

namespace Alpha.ViewComponents
{
    public class CarouselViewComponent : ViewComponent
    {
        private readonly ICarouselRepository _carouselRepository;
        private readonly LanguageService _localization;

        public CarouselViewComponent(ICarouselRepository carouselRepository, LanguageService localization)
        {
            _carouselRepository = carouselRepository;
            _localization = localization;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var carousels = await _carouselRepository.GetAllAsync();

            var carouselViewModel = new CarouselResourceViewModel
            {
                CarouselItems = carousels.Select(c =>
                {
                    var action = string.IsNullOrEmpty(c.CarouselLink) ? "Services" : c.CarouselLink;
                    var finalAction = ActionExists(action, "Home") ? action : "Services";

                    return new CarouselItemViewModel
                    {
                        CarouselId = c.CarouselId,
                        CarouselImage = c.CarouselImage,
                        CarouselImage600w = c.CarouselImage600w,
                        CarouselImage1200w = c.CarouselImage1200w,
                        CarouselTitle = _localization.Getkey($"Carousel_{c.CarouselId}_Title")?.Value ?? c.CarouselTitle,
                        CarouselDescription = _localization.Getkey($"Carousel_{c.CarouselId}_Description")?.Value ?? c.CarouselDescription,
                        CarouselLink = Url.Action(finalAction, "Home"), // Use the validated action
                        CarouselLinkText = _localization.Getkey($"Carousel_{c.CarouselId}_LinkText")?.Value ?? c.CarouselLinkText
                    };
                }).ToList()
            };

            return View(carouselViewModel);
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
