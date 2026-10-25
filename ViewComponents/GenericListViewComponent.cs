// GenericListViewComponent.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SpeakingClub.Data.Abstract;
using SpeakingClub.Identity;

namespace SpeakingClub.ViewComponents
{
    // The lists of the admin sections (Views/Admin/Section.cshtml). Each list now has a page of its own,
    // so counts that used to come for free from other lists loaded in the same request (EF relationship
    // fix-up on the old single page) are worked out here.
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

            switch (entityType.ToLower())
            {
                case "blogs":
                    return View("BlogList", (await _uow.Blogs.GetAllAsync()).OrderByDescending(b => b.Date).ToList());

                case "quizzes":
                    // How many results each quiz has: deleting a quiz deletes them too, and the dialog says so
                    ViewData["SubmissionCounts"] = (await _uow.QuizSubmissions.GetAllAsync())
                        .GroupBy(s => s.QuizId).ToDictionary(g => g.Key, g => g.Count());
                    return View("QuizList", (await _uow.Quizzes.GetAllAsync()).OrderBy(q => q.Title).ToList());

                case "questions":
                    return View("QuestionList", (await _uow.Questions.GetAllAsync())
                        .OrderBy(q => q.Quiz?.Title).ThenBy(q => q.Id).ToList());

                case "slides":
                    return View("SlideList", (await _uow.Slides.GetAllAsync()).OrderByDescending(s => s.DateAdded).ToList());

                case "tags":
                    return View("TagList", (await _uow.Tags.GetAllAsync()).OrderBy(t => t.Name).ToList());

                case "categories":
                    ViewData["BlogCounts"] = (await _uow.Blogs.GetAllAsync(b => b.CategoryId != null))
                        .GroupBy(b => b.CategoryId!.Value).ToDictionary(g => g.Key, g => g.Count());
                    ViewData["QuizCounts"] = (await _uow.Quizzes.GetAllAsync(q => q.CategoryId != null))
                        .GroupBy(q => q.CategoryId!.Value).ToDictionary(g => g.Key, g => g.Count());
                    return View("CategoryList", (await _uow.Categories.GetAllAsync()).OrderBy(c => c.Name).ToList());

                default:
                    return Content("Invalid entity type");
            }
        }
    }
}
