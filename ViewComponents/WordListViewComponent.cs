// WordListViewComponent.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using SpeakingClub.Data.Concrete;
using SpeakingClub.Entity;
using System.Collections.Generic;
using System.Threading.Tasks;
using SpeakingClub.Data.Abstract;
using SpeakingClub.Identity;

namespace SpeakingClub.ViewComponents
{
    public class WordListViewComponent : ViewComponent
    {
        private readonly IUnitOfWork _uow;
        private readonly UserManager<User> _userManager;

        public WordListViewComponent(IUnitOfWork uow, UserManager<User> userManager)
        {
            _uow = uow;
            _userManager = userManager;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var user = await _userManager.GetUserAsync(HttpContext.User);
            if (user == null || !(User.IsInRole("Root") || User.IsInRole("Admin") || User.IsInRole("Teacher")))
            {
                return Content(string.Empty);
            }

            var words = await _uow.Words.GetAllAsync();
            return View(words);
        }
    }
}