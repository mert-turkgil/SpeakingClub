// RoleManagementViewComponent.cs
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SpeakingClub.Identity;

namespace SpeakingClub.ViewComponents
{
    // Admin → Roller: each role with how many people have it. (The page is Admin/Root only —
    // AdminController.Roles; an [Authorize] attribute on a view component has no effect.)
    public class RoleManagementViewComponent : ViewComponent
    {
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly UserManager<User> _userManager;

        public RoleManagementViewComponent(RoleManager<IdentityRole> roleManager, UserManager<User> userManager)
        {
            _roleManager = roleManager;
            _userManager = userManager;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var roles = await _roleManager.Roles.OrderBy(r => r.Name).ToListAsync();
            var model = new List<(IdentityRole Role, int Users)>();
            foreach (var role in roles)
            {
                model.Add((role, (await _userManager.GetUsersInRoleAsync(role.Name!)).Count));
            }
            return View(model);
        }
    }
}
