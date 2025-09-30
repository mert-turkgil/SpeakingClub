// GenericListViewComponent.cs
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
    public class GenericListViewComponent : ViewComponent
    {
        private readonly IUnitOfWork _uow;
        private readonly UserManager<User> _userManager;

        public GenericListViewComponent(IUnitOfWork uow, UserManager<User> userManager)
        {
            _uow = uow;
            _userManager = userManager;
        }

        public async Task<IViewComponentResult> InvokeAsync(string entityType)
        {
            var user = await _userManager.GetUserAsync(HttpContext.User);
            if (user == null || !User.IsInRole("Root") && !User.IsInRole("Admin") && !User.IsInRole("Teacher"))
            {
                return Content(string.Empty);
            }

            return entityType.ToLower() switch
            {
                "blogs" => View("BlogList", await _uow.Blogs.GetAllAsync()),
                "categories" => View("CategoryList", await _uow.Categories.GetAllAsync()),
                "quizzes" => View("QuizList", await _uow.Quizzes.GetAllAsync()),
                "tags" => View("TagList", await _uow.Tags.GetAllAsync()),
                "questions" => View("QuestionList", await _uow.Questions.GetAllAsync()),
                "slides" => View("SlideList", await _uow.Slides.GetAllAsync()),
                _ => Content("Invalid entity type")
            };
        }
    }
}