using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using UI.Data.Abstract;
using UI.Services;

namespace UI.ViewComponents
{
    public class CardViewComponent : ViewComponent
    {
        #nullable disable
        private readonly IBlogRepository _blogRepository;
        private readonly LanguageService _localization;

        public CardViewComponent(IBlogRepository blogRepository, LanguageService localization)
        {
            _blogRepository = blogRepository;
            _localization = localization;
        }

        public async Task<IViewComponentResult> InvokeAsync(){
            return View();
        }

    }
}