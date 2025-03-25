// RoleManagementViewComponent.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace SpeakingClub.ViewComponents
{
    [Authorize(Roles = "Admin,Root")]
    public class RoleManagementViewComponent : ViewComponent
    {
        private readonly RoleManager<IdentityRole> _roleManager;

        public RoleManagementViewComponent(RoleManager<IdentityRole> roleManager)
        {
            _roleManager = roleManager;
        }
        public async Task<IViewComponentResult> InvokeAsync()
        {
            var roles = await _roleManager.Roles.ToListAsync();

            return View(roles);
        }
    }
}