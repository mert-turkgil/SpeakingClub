// UserManagementViewComponent.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SpeakingClub.Identity;
using SpeakingClub.Models;

namespace SpeakingClub.ViewComponents
{
    [Authorize(Roles = "Admin,Root")]
    public class UserManagementViewComponent : ViewComponent
    {
        private readonly UserManager<User> _userManager;

        public UserManagementViewComponent(UserManager<User> userManager)
        {
            _userManager = userManager;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var currentUser = await _userManager.GetUserAsync(HttpContext.User);
            if (currentUser == null)
            {
                // Handle the case where the user is not found
                return View(new List<UserListModel>());
            }

            var users = _userManager.Users.ToList();

            // Use async lambda and await all tasks
            var userTasks = users.Select(async user => new UserListModel
            {
                Id = user.Id!,
                Email = user.Email ?? string.Empty,
                IsLockedOut = await _userManager.IsLockedOutAsync(user)
            });

            var model = await Task.WhenAll(userTasks);

            // Convert the array to a list (if needed)
            return View(model.ToList());
        }
    }
}