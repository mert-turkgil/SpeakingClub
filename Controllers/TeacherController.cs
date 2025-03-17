using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SpeakingClub.Data;
using SpeakingClub.Identity;
using SpeakingClub.Models;
using System.Threading.Tasks;

namespace SpeakingClub.Controllers
{
    [Authorize(Roles = "Teacher")]
    public class TeacherController : Controller
    {
        private readonly SpeakingClubContext _context;
        private readonly UserManager<User> _userManager;

        public TeacherController(SpeakingClubContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Teacher/CreateQuiz
        public IActionResult CreateQuiz() => View();

        // POST: Teacher/CreateQuiz
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateQuiz(Quiz model)
        {
            if (ModelState.IsValid)
            {
                // Set the teacher (current user) as the creator.
                model.TeacherId = _userManager.GetUserId(User);
                await _context.SaveChangesAsync();
                return RedirectToAction("QuizList");
            }
            return View(model);
        }

        // Similarly, add actions to CreateBlog, EditQuiz, etc.
    }
}
