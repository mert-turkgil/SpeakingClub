// UserManagementViewComponent.cs
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SpeakingClub.Identity;
using SpeakingClub.Models;

namespace SpeakingClub.ViewComponents
{
    // Admin → Kullanıcılar. Roles are read once per role (a handful of queries) rather than once per user.
    // (The [Authorize] attribute this class had does nothing on a view component; the page itself is
    // Admin/Root only — AdminController.Users.)
    public class UserManagementViewComponent : ViewComponent
    {
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public UserManagementViewComponent(UserManager<User> userManager, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var currentUser = await _userManager.GetUserAsync(HttpContext.User);
            if (currentUser == null)
            {
                return View(new List<UserListModel>());
            }
            var isRoot = User.IsInRole("Root");

            var rolesByUser = new Dictionary<string, List<string>>();
            foreach (var role in _roleManager.Roles.Select(r => r.Name!).ToList())
            {
                foreach (var member in await _userManager.GetUsersInRoleAsync(role))
                {
                    if (!rolesByUser.TryGetValue(member.Id, out var list)) rolesByUser[member.Id] = list = new List<string>();
                    list.Add(role);
                }
            }

            var model = _userManager.Users.ToList().Select(user =>
            {
                var roles = rolesByUser.TryGetValue(user.Id, out var r) ? r : new List<string>();
                var privileged = roles.Contains("Root") || roles.Contains("Admin");
                var isSelf = user.Id == currentUser.Id;
                return new UserListModel
                {
                    Id = user.Id,
                    Email = user.Email,
                    UserName = user.UserName,
                    FullName = string.Join(' ', new[] { user.FirstName, user.LastName }.Where(p => !string.IsNullOrWhiteSpace(p))),
                    Roles = roles.OrderBy(x => x).ToList(),
                    EmailConfirmed = user.EmailConfirmed,
                    IsLockedOut = user.LockoutEnd.HasValue && user.LockoutEnd > DateTimeOffset.UtcNow,
                    IsSelf = isSelf,
                    CanEdit = !privileged || isRoot,
                    CanDelete = !isSelf && !roles.Contains("Root") && (!roles.Contains("Admin") || isRoot)
                };
            })
            .OrderByDescending(u => u.IsSelf)
            .ThenBy(u => u.Roles.Contains("User") && u.Roles.Count == 1)
            .ThenBy(u => string.IsNullOrEmpty(u.FullName) ? u.Email : u.FullName)
            .ToList();

            return View(model);
        }
    }
}
