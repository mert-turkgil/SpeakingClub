using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UI.Models;

namespace UI.Services
{
    public interface INavbarService
    {
        NavbarViewModel GetNavbarViewModel();
    }
}