using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UI.Data.Abstract;
using UI.Models;
using UI.Services;

namespace UI.Services
{
    public class NavbarService : INavbarService
    {
        private readonly LanguageService _localization;

        public NavbarService( LanguageService localization)
        {
            _localization = localization;
        }

        public NavbarViewModel GetNavbarViewModel()
        {
            return new NavbarViewModel
            {
                Logo = _localization.Getkey("Logo").Value,
                tanitim1 = _localization.Getkey("tanitim-1").Value,
                tanitim2 = _localization.Getkey("tanitim-2").Value,
                tanitim3 = _localization.Getkey("tanitim-3").Value
            };
        }
    }
}