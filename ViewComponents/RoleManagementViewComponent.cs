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
        private readonly ApplicationDbContext _identity;

        public RoleManagementViewComponent(RoleManager<IdentityRole> roleManager, ApplicationDbContext identity)
        {
            _roleManager = roleManager;
            _identity = identity;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var roles = await _roleManager.Roles.OrderBy(r => r.Name).ToListAsync();
            // Members per role counted in SQL (GetUsersInRoleAsync loaded every member just to count them)
            var counts = await _identity.UserRoles
                .GroupBy(ur => ur.RoleId)
                .Select(g => new { g.Key, Count = g.Count() })
                .ToDictionaryAsync(g => g.Key, g => g.Count);
            var model = roles.Select(role => (role, counts.TryGetValue(role.Id, out var n) ? n : 0)).ToList();
            return View(model);
        }
    }
}
