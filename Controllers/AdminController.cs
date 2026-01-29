using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;
using SpeakingClub.Data.Abstract;
using SpeakingClub.Entity;
using SpeakingClub.Identity;
using SpeakingClub.Models;
using SpeakingClub.Services;

namespace SpeakingClub.Controllers
{
    [Authorize(Roles = "Root,Admin,Teacher")]
    [Route("[controller]")]
    public class AdminController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly IUnitOfWork _unitOfWork;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ILogger<AdminController> _logger;
        private readonly IManageResourceService _manageResourceService;
        private readonly IWebHostEnvironment _env;
        private readonly AliveResourceService _dynamicResourceService;

        public AdminController(ILogger<AdminController> logger, UserManager<User> userManager, RoleManager<IdentityRole> roleManager, IUnitOfWork unitOfWork, IWebHostEnvironment env,
        IManageResourceService manageResourceService, AliveResourceService dynamicResourceService)
        {
            _env = env;
            _dynamicResourceService = dynamicResourceService;
            _manageResourceService = manageResourceService;
            _roleManager = roleManager;
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _logger = logger;
        }
        [HttpGet("")]
        public IActionResult Index()
        {
            return View();
        }

        // HomeController.cs (Updated methods)
        #region Hesap
        [HttpGet("UserCreate")]
        [Authorize(Roles = "Admin,Root")]
        public IActionResult UserCreate()
        {
            var model = new UserCreateModel
            {
                FirstName = "",
                LastName = "",
                UserName = "",
                Email = "",
                Password = "",
                ConfirmPassword = "",
                AllRoles = _roleManager.Roles.Select(r => r.Name!).ToList(),
                EmailConfirmed = false,
                LockoutEnabled = false
            };
            return View(model);
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Root")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UserCreate(UserCreateModel model)
        {
            if (!ModelState.IsValid)
            {
                model.AllRoles = _roleManager.Roles.Select(r => r.Name!).ToList();
                return View(model);
            }

            var user = new SpeakingClub.Identity.User
            {
                FirstName = model.FirstName,
                LastName = model.LastName,
                Age = model.Age,
                Email = model.Email,
                UserName = model.UserName,
                EmailConfirmed = model.EmailConfirmed,
                LockoutEnabled = model.LockoutEnabled,
                LockoutEnd = model.LockoutEnabled ? model.LockoutEnd : null
            };

            var createResult = await _userManager.CreateAsync(user, model.Password);

            if (!createResult.Succeeded)
            {
                foreach (var error in createResult.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
                model.AllRoles = _roleManager.Roles.Select(r => r.Name!).ToList();
                return View(model);
            }

            if (model.SelectedRoles != null && model.SelectedRoles.Any())
            {
                var roleResult = await _userManager.AddToRolesAsync(user, model.SelectedRoles);
                if (!roleResult.Succeeded)
                {
                    foreach (var error in roleResult.Errors)
                    {
                        ModelState.AddModelError("", error.Description);
                    }
                    model.AllRoles = _roleManager.Roles.Select(r => r.Name!).ToList();
                    return View(model);
                }
            }

            TempData["SuccessMessage"] = "User created successfully!";
            return RedirectToAction("Index"); // Adjust to your needs
        }
        [Authorize(Roles = "Admin,Root")]
        [HttpPost("UserDelete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UserDelete(string id)
        {
            // Find the user by ID
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction("Index");
            }

            // Get the current logged-in user
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                TempData["ErrorMessage"] = "Current user not found.";
                return RedirectToAction("Index");
            }

            // Check if the target user is the Root user
            bool isTargetRoot = await _userManager.IsInRoleAsync(user, "Root");
            bool isTargetAdmin = await _userManager.IsInRoleAsync(user, "Admin");

            // Prevent deletion if:
            // 1. Target is Root (regardless of who tries to delete)
            // 2. Target is Admin and current user is not Root
            if (isTargetRoot || (isTargetAdmin && !User.IsInRole("Root")))
            {
                TempData["ErrorMessage"] = isTargetRoot
                    ? "You cannot delete the Root user."
                    : "Only Root users can delete Admin users.";
                return RedirectToAction("Index");
            }

            // Additional protection: Don't allow users to delete themselves
            if (user.Id == currentUser.Id)
            {
                TempData["ErrorMessage"] = "You cannot delete your own account.";
                return RedirectToAction("Index");
            }

            // Proceed to delete the user
            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "User deleted successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = "Error occurred while deleting the user: " +
                    string.Join(", ", result.Errors.Select(e => e.Description));
            }

            return RedirectToAction("Index");
        }

        // Edit User Method (GET)
        [HttpGet("UserEdit/{id}")]
        [Authorize(Roles = "Admin,Root")]
        public async Task<IActionResult> UserEdit(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction("Account");
            }

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                TempData["ErrorMessage"] = "Current user not found.";
                return RedirectToAction("Account");
            }

            // Check roles
            bool isTargetRoot = await _userManager.IsInRoleAsync(user, "Root");
            bool isTargetAdmin = await _userManager.IsInRoleAsync(user, "Admin");
            bool isCurrentRoot = await _userManager.IsInRoleAsync(currentUser, "Root");
            bool isCurrentAdmin = await _userManager.IsInRoleAsync(currentUser, "Admin");

            // Security checks
            if (isTargetRoot && !isCurrentRoot)
            {
                TempData["ErrorMessage"] = "Only Root can edit Root users.";
                return RedirectToAction("Account");
            }

            if (isTargetAdmin && !isCurrentRoot)
            {
                TempData["ErrorMessage"] = "Only Root can edit Admin users.";
                return RedirectToAction("Account");
            }

            // Prevent self-editing of critical fields
            bool isSelf = user.Id == currentUser.Id;

            var model = new UserEditModel
            {
                UserId = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Age = user.Age,
                Email = user.Email!,
                UserName = user.UserName!,
                EmailConfirmed = user.EmailConfirmed,
                Lockout = user.LockoutEnabled && user.LockoutEnd > DateTimeOffset.UtcNow,
                SelectedRoles = (await _userManager.GetRolesAsync(user)).ToList(),
                AllRoles = _roleManager.Roles.Select(r => r.Name!).ToList(),
                IsSelf = isSelf  // Add this to disable certain fields in view for self-editing
            };

            return View(model);
        }

        // Edit User Method (POST)
        [HttpPost("UserEdit/{id}")]
        [Authorize(Roles = "Admin,Root")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UserEdit(UserEditModel model)
        {
            if (!ModelState.IsValid)
            {
                model.AllRoles = _roleManager.Roles.Select(r => r.Name!).ToList();
                return View(model);
            }

            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction("Account");
            }

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                TempData["ErrorMessage"] = "Current user not found.";
                return RedirectToAction("Account");
            }

            // Check roles
            bool isTargetRoot = await _userManager.IsInRoleAsync(user, "Root");
            bool isTargetAdmin = await _userManager.IsInRoleAsync(user, "Admin");
            bool isCurrentRoot = await _userManager.IsInRoleAsync(currentUser, "Root");
            bool isCurrentAdmin = await _userManager.IsInRoleAsync(currentUser, "Admin");

            // Security checks
            if (isTargetRoot && !isCurrentRoot)
            {
                TempData["ErrorMessage"] = "Only Root can edit Root users.";
                return RedirectToAction("Account");
            }

            if (isTargetAdmin && !isCurrentRoot)
            {
                TempData["ErrorMessage"] = "Only Root can edit Admin users.";
                return RedirectToAction("Account");
            }

            // Prevent self-editing of critical fields
            bool isSelf = user.Id == currentUser.Id;

            // Update basic info
            user.FirstName = model.FirstName;
            user.LastName = model.LastName;
            user.Age = model.Age;
            user.Email = model.Email;
            user.UserName = model.UserName;

            // Only allow email confirmation changes for non-self edits
            if (!isSelf)
            {
                user.EmailConfirmed = model.EmailConfirmed;
            }

            // Lockout settings (only for non-self and non-root users)
            if (!isSelf && !isTargetRoot)
            {
                user.LockoutEnabled = model.Lockout;
                user.LockoutEnd = model.Lockout ? DateTimeOffset.MaxValue : null;
            }

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                AddErrors(updateResult);
                model.AllRoles = _roleManager.Roles.Select(r => r.Name!).ToList();
                return View(model);
            }

            // Role management (only for non-self and non-root users)
            if (!isSelf && !isTargetRoot)
            {
                var currentRoles = await _userManager.GetRolesAsync(user);
                var rolesToRemove = currentRoles.Except(model.SelectedRoles ?? new List<string>());
                var rolesToAdd = (model.SelectedRoles ?? new List<string>()).Except(currentRoles);

                await _userManager.RemoveFromRolesAsync(user, rolesToRemove);
                await _userManager.AddToRolesAsync(user, rolesToAdd);
            }

            // Password reset (only for non-self edits)
            if (!isSelf && !string.IsNullOrWhiteSpace(model.Password))
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var passwordResult = await _userManager.ResetPasswordAsync(user, token, model.Password);
                if (!passwordResult.Succeeded)
                {
                    AddErrors(passwordResult);
                    model.AllRoles = _roleManager.Roles.Select(r => r.Name!).ToList();
                    return View(model);
                }
            }

            TempData["SuccessMessage"] = "User updated successfully!";
            return RedirectToAction("Account");
        }

        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }

        #endregion

        #region  Roles
        // GET: Role Create
        [HttpGet("RoleCreate")]
        public IActionResult RoleCreate()
        {
            return View();
        }

        // POST: Role Create
        [HttpPost("RoleCreate")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RoleCreate(RoleCreateModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var role = new IdentityRole(model.Name);
            var result = await _roleManager.CreateAsync(role);

            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = $"Role '{model.Name}' created successfully!";
                return RedirectToAction("Index");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        // GET: Role Edit
        [HttpGet("RoleEdit/{id}")]
        public async Task<IActionResult> RoleEdit(string id)
        {
            var role = await _roleManager.FindByIdAsync(id);
            if (role == null)
            {
                TempData["ErrorMessage"] = "Role not found";
                return RedirectToAction("Index");
            }

            var model = new RoleEditModel
            {
                Id = role.Id,
                Name = role.Name!
            };

            return View(model);
        }

        // POST: Role Edit
        [HttpPost("RoleEdit/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RoleEdit(RoleEditModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var role = await _roleManager.FindByIdAsync(model.Id);
            if (role == null)
            {
                TempData["ErrorMessage"] = "Role not found";
                return RedirectToAction("Index");
            }

            role.Name = model.Name;
            var result = await _roleManager.UpdateAsync(role);

            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = $"Role '{model.Name}' updated successfully!";
                return RedirectToAction("Index");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        // POST: Role Delete
        [HttpPost("RoleDelete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RoleDelete(string id)
        {
            var role = await _roleManager.FindByIdAsync(id);
            if (role == null)
            {
                TempData["ErrorMessage"] = "Role not found";
                return RedirectToAction("Index");
            }

            // Prevent deletion of essential roles
            if (role.Name == "Root" || role.Name == "Admin")
            {
                TempData["ErrorMessage"] = $"Cannot delete system role '{role.Name}'";
                return RedirectToAction("Index");
            }

            var result = await _roleManager.DeleteAsync(role);
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = $"Role '{role.Name}' deleted successfully!";
            }
            else
            {
                TempData["ErrorMessage"] = $"Error deleting role '{role.Name}'";
            }

            return RedirectToAction("Index");
        }
        #endregion

        #region Services
        [HttpPost("EditTranslation")]
        [ValidateAntiForgeryToken]
        public IActionResult EditTranslation(string name, string value, string comment, string lang)
        {
            if (string.IsNullOrEmpty(name))
                return Json(new { success = false, message = "Translation key is required." });

            var resxPath = GetResxPath(lang);

            if (!System.IO.File.Exists(resxPath))
                return Json(new { success = false, message = "Localization file not found." });

            var resxFile = XDocument.Load(resxPath);

            var dataElement = resxFile.Root?
                .Elements("data")
                .FirstOrDefault(x => x.Attribute("name")?.Value == name);

            if (dataElement != null)
            {
                var valueElement = dataElement.Element("value");
                if (valueElement != null)
                {
                    valueElement.Value = value ?? string.Empty;
                }
                else
                {
                    dataElement.Add(new XElement("value", value ?? string.Empty));
                }

                var commentElement = dataElement.Element("comment");
                if (commentElement != null)
                {
                    commentElement.Value = comment ?? string.Empty;
                }
                else
                {
                    dataElement.Add(new XElement("comment", comment ?? string.Empty));
                }

                resxFile.Save(resxPath);
                
                // Clear the resource cache so changes take effect immediately
                _dynamicResourceService.ReloadResources();
                
                return Json(new { success = true, message = "Translation updated successfully." });
            }

            return Json(new { success = false, message = "Translation key not found." });
        }
        private string GetResxPath(string lang)
        {
            var fileName = $"SharedResource.{lang}.resx";
            return Path.Combine(Directory.GetCurrentDirectory(), "Resources", fileName);
        }

        #endregion

        #region Tag
        // Tag Edit (GET)
        [HttpGet("TagEdit/{id}")]
        public async Task<IActionResult> TagEdit(int id)
        {
            var tag = await _unitOfWork.Tags.GetByIdAsync(id);
            if (tag == null)
                return NotFound();

            // Fetch all blogs and quizzes for the admin to choose from
            var allBlogs = await _unitOfWork.Blogs.GetAllAsync();
            var allQuizzes = await _unitOfWork.Quizzes.GetAllAsync();

            var model = new TagEditViewModel
            {
                TagId = tag.TagId,
                Name = tag.Name,
                SelectedBlogIds = tag.Blogs.Select(b => b.BlogId).ToList(),
                SelectedQuizIds = tag.Quizzes.Select(q => q.Id).ToList(),
                AvailableBlogs = allBlogs.Select(b => new SelectListItem { Value = b.BlogId.ToString(), Text = b.Title }),
                AvailableQuizzes = allQuizzes.Select(q => new SelectListItem { Value = q.Id.ToString(), Text = q.Title })
            };
            return View(model);
        }


        [HttpPost("TagEdit/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TagEdit(TagEditViewModel model)
        {
            if (!ModelState.IsValid)
            {
                // Re-populate selection lists if validation fails
                model.AvailableBlogs = (await _unitOfWork.Blogs.GetAllAsync())
                    .Select(b => new SelectListItem { Value = b.BlogId.ToString(), Text = b.Title });
                model.AvailableQuizzes = (await _unitOfWork.Quizzes.GetAllAsync())
                    .Select(q => new SelectListItem { Value = q.Id.ToString(), Text = q.Title });
                return View(model);
            }

            var tag = await _unitOfWork.Tags.GetByIdAsync(model.TagId);
            if (tag == null)
                return NotFound();

            tag.Name = model.Name;

            // --- Manage Blog associations ---
            var allBlogs = await _unitOfWork.Blogs.GetAllAsync();
            tag.Blogs = allBlogs.Where(b => model.SelectedBlogIds.Contains(b.BlogId)).ToList();

            // --- Manage Quiz associations ---
            var allQuizzes = await _unitOfWork.Quizzes.GetAllAsync();
            tag.Quizzes = allQuizzes.Where(q => model.SelectedQuizIds.Contains(q.Id)).ToList();

            _unitOfWork.Tags.Update(tag);
            await _unitOfWork.SaveAsync();

            TempData["SuccessMessage"] = "Tag updated successfully!";
            return RedirectToAction("Index");
        }


        [HttpPost("TagDelete/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TagDelete(int id)
        {
            var tag = await _unitOfWork.Tags.GetByIdAsync(id);
            if (tag == null)
            {
                TempData["ErrorMessage"] = "Tag not found.";
                return RedirectToAction("Index");
            }

            // Remove tag associations with blogs and quizzes
            foreach (var blog in tag.Blogs.ToList())
                blog.Tags.Remove(tag);
            foreach (var quiz in tag.Quizzes.ToList())
                quiz.Tags.Remove(tag);

            _unitOfWork.Tags.Remove(tag);
            await _unitOfWork.SaveAsync();

            TempData["SuccessMessage"] = "Tag deleted successfully!";
            return RedirectToAction("Index"); // or TagList
        }


        [HttpGet("TagCreate")]
        public IActionResult TagCreate()
        {
            var model = new TagCreateViewModel();
            return View(model);
        }

        [HttpPost("TagCreate")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TagCreate(TagCreateViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var tag = new Tag { Name = model.Name };

            // Optionally check for duplicates
            var exists = (await _unitOfWork.Tags.GetAllAsync()).Any(t => t.Name == model.Name);
            if (exists)
            {
                ModelState.AddModelError("Name", "This tag already exists.");
                return View(model);
            }

            await _unitOfWork.Tags.AddAsync(tag);
            await _unitOfWork.SaveAsync();

            TempData["SuccessMessage"] = "Tag created successfully!";
            return RedirectToAction("Index"); // Or TagList if you have a list page
        }

        #endregion

        #region Category
        [HttpGet("CategoryEdit/{id}")]
        public async Task<IActionResult> CategoryEdit(int id)
        {
            var category = await _unitOfWork.Categories.GetByIdAsync(id);
            if (category == null)
                return NotFound();

            var allBlogs = await _unitOfWork.Blogs.GetAllAsync();
            var allQuizzes = await _unitOfWork.Quizzes.GetAllAsync();

            var model = new CategoryEditViewModel
            {
                CategoryId = category.CategoryId,
                Name = category.Name,
                SelectedBlogIds = category.Blogs?.Select(b => b.BlogId).ToList() ?? new List<int>(),
                SelectedQuizIds = category.Quizzes?.Select(q => q.Id).ToList() ?? new List<int>(),
                AvailableBlogs = allBlogs.Select(b => new SelectListItem { Value = b.BlogId.ToString(), Text = b.Title }),
                AvailableQuizzes = allQuizzes.Select(q => new SelectListItem { Value = q.Id.ToString(), Text = q.Title })
            };
            return View(model);
        }

        [HttpPost("CategoryEdit/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CategoryEdit(CategoryEditViewModel model)
        {
            if (!ModelState.IsValid)
            {
                // Repopulate selections if validation fails
                model.AvailableBlogs = (await _unitOfWork.Blogs.GetAllAsync())
                    .Select(b => new SelectListItem { Value = b.BlogId.ToString(), Text = b.Title });
                model.AvailableQuizzes = (await _unitOfWork.Quizzes.GetAllAsync())
                    .Select(q => new SelectListItem { Value = q.Id.ToString(), Text = q.Title });
                return View(model);
            }

            var category = await _unitOfWork.Categories.GetByIdAsync(model.CategoryId);
            if (category == null)
                return NotFound();

            category.Name = model.Name;

            // Update Blog associations
            var allBlogs = await _unitOfWork.Blogs.GetAllAsync();
            category.Blogs = allBlogs.Where(b => model.SelectedBlogIds.Contains(b.BlogId)).ToList();

            // Update Quiz associations
            var allQuizzes = await _unitOfWork.Quizzes.GetAllAsync();
            category.Quizzes = allQuizzes.Where(q => model.SelectedQuizIds.Contains(q.Id)).ToList();

            _unitOfWork.Categories.Update(category);
            await _unitOfWork.SaveAsync();

            TempData["SuccessMessage"] = "Category updated successfully!";
            return RedirectToAction("Index");
        }

        [HttpGet("CategoryCreate")]
        public async Task<IActionResult> CategoryCreate()
        {
            var allBlogs = await _unitOfWork.Blogs.GetAllAsync();
            var allQuizzes = await _unitOfWork.Quizzes.GetAllAsync();

            var model = new CategoryCreateViewModel
            {
                AvailableBlogs = allBlogs.Select(b => new SelectListItem { Value = b.BlogId.ToString(), Text = b.Title }),
                AvailableQuizzes = allQuizzes.Select(q => new SelectListItem { Value = q.Id.ToString(), Text = q.Title })
            };
            return View(model);
        }

        [HttpPost("CategoryCreate")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CategoryCreate(CategoryCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.AvailableBlogs = (await _unitOfWork.Blogs.GetAllAsync())
                    .Select(b => new SelectListItem { Value = b.BlogId.ToString(), Text = b.Title });
                model.AvailableQuizzes = (await _unitOfWork.Quizzes.GetAllAsync())
                    .Select(q => new SelectListItem { Value = q.Id.ToString(), Text = q.Title });
                return View(model);
            }

            var category = new Category
            {
                Name = model.Name
            };

            // Associate Blogs and Quizzes
            var allBlogs = await _unitOfWork.Blogs.GetAllAsync();
            category.Blogs = allBlogs.Where(b => model.SelectedBlogIds.Contains(b.BlogId)).ToList();

            var allQuizzes = await _unitOfWork.Quizzes.GetAllAsync();
            category.Quizzes = allQuizzes.Where(q => model.SelectedQuizIds.Contains(q.Id)).ToList();

            await _unitOfWork.Categories.AddAsync(category);
            await _unitOfWork.SaveAsync();

            TempData["SuccessMessage"] = "Category created successfully!";
            return RedirectToAction("Index");
        }

        [HttpPost("CategoryDelete/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CategoryDelete(int id)
        {
            var category = await _unitOfWork.Categories.GetByIdAsync(id);
            if (category == null)
            {
                TempData["ErrorMessage"] = "Category not found.";
                return RedirectToAction("Index");
            }

            // Remove associations (optional: depends on cascade behavior)
            foreach (var blog in category.Blogs?.ToList() ?? new List<Blog>())
                blog.CategoryId = null;

            foreach (var quiz in category.Quizzes?.ToList() ?? new List<Entity.Quiz>())
                quiz.CategoryId = null;

            _unitOfWork.Categories.Remove(category);
            await _unitOfWork.SaveAsync();

            TempData["SuccessMessage"] = "Category deleted successfully!";
            return RedirectToAction("Index");
        }


        #endregion


        #region Blog Management

        [HttpGet("BlogCreate")]
        public async Task<IActionResult> BlogCreate()
        {
            var model = new BlogCreateModel
            {
                TempBlogId = Guid.NewGuid().ToString(),
                Date = DateTime.Now,
                IsPublished = true,
                NoIndex = false,
                NoFollow = false
            };
            
            ViewBag.Categories = (await _unitOfWork.Categories.GetAllAsync())
                .Select(c => new SelectListItem(c.Name, c.CategoryId.ToString()));
            ViewBag.Tags = (await _unitOfWork.Tags.GetAllAsync())
                .Select(t => new SelectListItem(t.Name, t.TagId.ToString()));
            ViewBag.Quizzes = (await _unitOfWork.Quizzes.GetAllAsync())
                .Select(q => new SelectListItem(q.Title, q.Id.ToString()));
                
            return View(model);
        }

        [HttpPost("BlogCreate")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BlogCreate(BlogCreateModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Categories = (await _unitOfWork.Categories.GetAllAsync())
                    .Select(c => new SelectListItem(c.Name, c.CategoryId.ToString()));
                ViewBag.Tags = (await _unitOfWork.Tags.GetAllAsync())
                    .Select(t => new SelectListItem(t.Name, t.TagId.ToString()));
                ViewBag.Quizzes = (await _unitOfWork.Quizzes.GetAllAsync())
                    .Select(q => new SelectListItem(q.Title, q.Id.ToString()));
                return View(model);
            }

            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                // Generate slug
                var slug = !string.IsNullOrEmpty(model.Slug) 
                    ? GenerateSlug(model.Slug) 
                    : GenerateSlug(model.Title);
                slug = await EnsureUniqueSlugAsync(slug, null);

                // Create blog entity
                var blog = new Blog
                {
                    Title = model.Title,
                    Description = model.Description ?? string.Empty,
                    Content = model.Content ?? string.Empty,
                    
                    // SEO Fields
                    Slug = slug,
                    MetaDescription = !string.IsNullOrEmpty(model.MetaDescription) 
                        ? model.MetaDescription 
                        : (model.Description?.Length > 160 ? model.Description.Substring(0, 160) : model.Description ?? ""),
                    MetaKeywords = model.MetaKeywords ?? string.Empty,
                    MetaTitle = model.MetaTitle ?? model.Title,
                    FocusKeyphrase = model.FocusKeyphrase ?? string.Empty,
                    CanonicalUrl = !string.IsNullOrEmpty(model.CanonicalUrl) 
                        ? model.CanonicalUrl 
                        : $"https://almanca-konus.com/blog/{slug}",
                    NoIndex = model.NoIndex,
                    NoFollow = model.NoFollow,
                    
                    // Open Graph
                    OgTitle = model.OgTitle ?? model.Title,
                    OgDescription = model.OgDescription ?? model.MetaDescription ?? model.Description ?? string.Empty,
                    OgImage = string.Empty,
                    
                    // Basic fields
                    Url = model.Url ?? slug,
                    Author = model.Author,
                    Date = model.Date,
                    isHome = model.IsHome,
                    IsPublished = model.IsPublished,
                    RawYT = model.RawYT ?? string.Empty,
                    RawMaps = model.RawMaps ?? string.Empty,
                    SelectedQuestionId = model.SelectedQuestionId,
                    LastModified = DateTime.UtcNow,
                    ViewCount = 0
                };

                // Handle cover image
                if (model.ImageFile != null && model.ImageFile.Length > 0)
                {
                    blog.Image = await SaveBlogCoverImageAsync(model.ImageFile);
                    blog.OgImage = $"https://almanca-konus.com/img/{blog.Image}";
                }

                // Save blog first to get ID
                await _unitOfWork.Blogs.AddAsync(blog);
                await _unitOfWork.SaveAsync();

                // Handle category
                if (model.SelectedCategoryIds != null && model.SelectedCategoryIds.Any())
                {
                    blog.CategoryId = model.SelectedCategoryIds.First();
                }

                // Handle tags
                if (model.SelectedTagIds != null && model.SelectedTagIds.Any())
                {
                    var allTags = await _unitOfWork.Tags.GetAllAsync();
                    var selectedTags = allTags.Where(t => model.SelectedTagIds.Contains(t.TagId)).ToList();
                    
                    foreach (var tag in selectedTags)
                    {
                        blog.Tags.Add(tag);
                    }
                }

                // Save translations
                await SaveBlogTranslationsAsync(blog.BlogId, model);

                _unitOfWork.Blogs.Update(blog);
                await _unitOfWork.SaveAsync();
                await transaction.CommitAsync();

                TempData["SuccessMessage"] = "Blog created successfully!";
                _logger.LogInformation("Blog {BlogId} '{Title}' created successfully", blog.BlogId, blog.Title);
                
                return RedirectToAction("Index", new { scrollTo = "BlogManagement" });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error creating blog");
                TempData["ErrorMessage"] = $"Error creating blog: {ex.Message}";
                
                ViewBag.Categories = (await _unitOfWork.Categories.GetAllAsync())
                    .Select(c => new SelectListItem(c.Name, c.CategoryId.ToString()));
                ViewBag.Tags = (await _unitOfWork.Tags.GetAllAsync())
                    .Select(t => new SelectListItem(t.Name, t.TagId.ToString()));
                ViewBag.Quizzes = (await _unitOfWork.Quizzes.GetAllAsync())
                    .Select(q => new SelectListItem(q.Title, q.Id.ToString()));
                return View(model);
            }
        }

        [HttpGet("BlogEdit/{id}")]
        public async Task<IActionResult> BlogEdit(int id)
        {
            var blog = await _unitOfWork.Blogs.GetAsync(id);
            if (blog == null)
            {
                TempData["ErrorMessage"] = "Blog not found.";
                return RedirectToAction("Index");
            }

            // Load translations
            var translations = await _unitOfWork.BlogTranslations.GetAllAsync(bt => bt.BlogId == id);
            var trTranslation = translations.FirstOrDefault(t => t.LanguageCode == "tr");
            var deTranslation = translations.FirstOrDefault(t => t.LanguageCode == "de");

            var model = new BlogEditModel
            {
                BlogId = blog.BlogId,
                Title = blog.Title,
                Description = blog.Description,
                Content = blog.Content,
                
                // SEO Fields
                Slug = blog.Slug,
                MetaDescription = blog.MetaDescription,
                MetaKeywords = blog.MetaKeywords,
                MetaTitle = blog.MetaTitle,
                FocusKeyphrase = blog.FocusKeyphrase,
                CanonicalUrl = blog.CanonicalUrl,
                NoIndex = blog.NoIndex,
                NoFollow = blog.NoFollow,
                
                // Open Graph
                OgTitle = blog.OgTitle,
                OgDescription = blog.OgDescription,
                OgImage = blog.OgImage,
                
                // Basic fields
                Url = blog.Url,
                Author = blog.Author,
                Date = blog.Date,
                IsHome = blog.isHome,
                IsPublished = blog.IsPublished,
                RawYT = blog.RawYT,
                RawMaps = blog.RawMaps,
                CoverImageUrl = blog.Image,
                SelectedQuestionId = blog.SelectedQuestionId,
                
                // Translations
                TitleTR = trTranslation?.Title,
                DescriptionTR = trTranslation?.Description,
                ContentTR = trTranslation?.Content,
                SlugTR = trTranslation?.Slug,
                MetaDescriptionTR = trTranslation?.MetaDescription,
                MetaKeywordsTR = trTranslation?.MetaKeywords,
                MetaTitleTR = trTranslation?.MetaTitle,
                OgTitleTR = trTranslation?.OgTitle,
                OgDescriptionTR = trTranslation?.OgDescription,
                
                TitleDE = deTranslation?.Title,
                DescriptionDE = deTranslation?.Description,
                ContentDE = deTranslation?.Content,
                SlugDE = deTranslation?.Slug,
                MetaDescriptionDE = deTranslation?.MetaDescription,
                MetaKeywordsDE = deTranslation?.MetaKeywords,
                MetaTitleDE = deTranslation?.MetaTitle,
                OgTitleDE = deTranslation?.OgTitle,
                OgDescriptionDE = deTranslation?.OgDescription,
                
                SelectedCategoryIds = blog.CategoryId.HasValue ? new List<int> { blog.CategoryId.Value } : new List<int>(),
                SelectedTagIds = blog.Tags?.Select(t => t.TagId).ToList() ?? new List<int>()
            };
            // Determine selected quiz based on stored SelectedQuestionId
            int? selectedQuizId = null;
            if (model.SelectedQuestionId.HasValue)
            {
                var currentQuestion = await _unitOfWork.Questions.GetAsync(model.SelectedQuestionId.Value);
                selectedQuizId = currentQuestion?.QuizId;
            }

            // Populate ViewBag for selects
            var allCategories = await _unitOfWork.Categories.GetAllAsync();
            ViewBag.Categories = allCategories.Select(c => new SelectListItem(c.Name, c.CategoryId.ToString()));

            var allTags = await _unitOfWork.Tags.GetAllAsync();
            ViewBag.Tags = allTags.Select(t => new SelectListItem(t.Name, t.TagId.ToString()));

            var allQuizzes = (await _unitOfWork.Quizzes.GetAllAsync()).ToList();
            ViewBag.Quizzes = allQuizzes
                .Select(q => new SelectListItem(q.Title, q.Id.ToString(), selectedQuizId.HasValue && q.Id == selectedQuizId.Value))
                .ToList();

            // Questions for the selected quiz (prepopulate on edit)
            if (selectedQuizId.HasValue)
            {
                var questions = (await _unitOfWork.Questions.GetQuestionsByQuizIdAsync(selectedQuizId.Value)).ToList();
                ViewBag.Questions = questions
                    .Select(q => new SelectListItem(q.QuestionText, q.Id.ToString(), model.SelectedQuestionId.HasValue && q.Id == model.SelectedQuestionId.Value))
                    .ToList();
            }
            else
            {
                ViewBag.Questions = new List<SelectListItem>();
            }

            return View(model);
        }

        [HttpPost("BlogEdit/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BlogEdit(BlogEditModel model)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("BlogEdit ModelState invalid");
                ViewBag.Categories = (await _unitOfWork.Categories.GetAllAsync())
                    .Select(c => new SelectListItem(c.Name, c.CategoryId.ToString()));
                ViewBag.Tags = (await _unitOfWork.Tags.GetAllAsync())
                    .Select(t => new SelectListItem(t.Name, t.TagId.ToString()));
                ViewBag.Quizzes = (await _unitOfWork.Quizzes.GetAllAsync())
                    .Select(q => new SelectListItem(q.Title, q.Id.ToString()));
                return View(model);
            }

            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var blog = await _unitOfWork.Blogs.GetAsync(model.BlogId);
                if (blog == null)
                {
                    TempData["ErrorMessage"] = "Blog not found.";
                    return RedirectToAction("Index");
                }

                // Generate/validate slug
                var newSlug = !string.IsNullOrEmpty(model.Slug) 
                    ? GenerateSlug(model.Slug) 
                    : GenerateSlug(model.Title);
                newSlug = await EnsureUniqueSlugAsync(newSlug, blog.BlogId);

                // Update blog entity
                blog.Title = model.Title;
                blog.Description = model.Description ?? string.Empty;
                blog.Content = model.Content ?? string.Empty;
                blog.Slug = newSlug;
                blog.MetaDescription = model.MetaDescription ?? string.Empty;
                blog.MetaKeywords = model.MetaKeywords ?? string.Empty;
                blog.MetaTitle = model.MetaTitle ?? model.Title;
                blog.FocusKeyphrase = model.FocusKeyphrase ?? string.Empty;
                blog.CanonicalUrl = !string.IsNullOrEmpty(model.CanonicalUrl) 
                    ? model.CanonicalUrl 
                    : $"https://almanca-konus.com/blog/{newSlug}";
                blog.NoIndex = model.NoIndex;
                blog.NoFollow = model.NoFollow;
                blog.OgTitle = model.OgTitle ?? model.Title;
                blog.OgDescription = model.OgDescription ?? model.MetaDescription ?? string.Empty;
                
                blog.Url = model.Url ?? newSlug;
                blog.Author = model.Author;
                blog.Date = model.Date;
                blog.isHome = model.IsHome;
                blog.IsPublished = model.IsPublished;
                blog.RawYT = model.RawYT ?? string.Empty;
                blog.RawMaps = model.RawMaps ?? string.Empty;
                blog.SelectedQuestionId = model.SelectedQuestionId;
                blog.LastModified = DateTime.UtcNow;

                // Handle cover image update
                if (model.ImageFile != null && model.ImageFile.Length > 0)
                {
                    if (!string.IsNullOrEmpty(blog.Image))
                    {
                        await DeleteBlogFileAsync(blog.Image);
                    }
                    
                    blog.Image = await SaveBlogCoverImageAsync(model.ImageFile);
                    blog.OgImage = $"https://almanca-konus.com/img/{blog.Image}";
                }
                else if (!string.IsNullOrEmpty(blog.Image))
                {
                    blog.OgImage = $"https://almanca-konus.com/img/{blog.Image}";
                }

                // Update category
                if (model.SelectedCategoryIds != null && model.SelectedCategoryIds.Any())
                {
                    blog.CategoryId = model.SelectedCategoryIds.First();
                }
                else
                {
                    blog.CategoryId = null;
                }

                // Update tags - FIXED APPROACH
                var selectedTagIds = (model.SelectedTagIds ?? new List<int>()).Distinct().ToList();
                var currentTagIds = blog.Tags?.Select(t => t.TagId).ToList() ?? new List<int>();
                
                // Load all tags with tracking
                var allTags = (await _unitOfWork.Tags.GetAllAsync()).ToList();
                
                // Remove tags that are no longer selected
                var tagsToRemove = blog.Tags?.Where(t => !selectedTagIds.Contains(t.TagId)).ToList() ?? new List<Tag>();
                foreach (var tag in tagsToRemove)
                {
                    blog.Tags?.Remove(tag);
                    _logger.LogInformation("Removed tag {TagId} from blog {BlogId}", tag.TagId, blog.BlogId);
                }
                
                // Add newly selected tags
                var tagsToAdd = allTags.Where(t => selectedTagIds.Contains(t.TagId) && !currentTagIds.Contains(t.TagId)).ToList();
                foreach (var tag in tagsToAdd)
                {
                    blog.Tags?.Add(tag);
                    _logger.LogInformation("Added tag {TagId} to blog {BlogId}", tag.TagId, blog.BlogId);
                }

                // Update translations
                await SaveBlogTranslationsAsync(blog.BlogId, model);

                _unitOfWork.Blogs.Update(blog);
                await _unitOfWork.SaveAsync();
                await transaction.CommitAsync();

                TempData["SuccessMessage"] = "Blog updated successfully!";
                _logger.LogInformation("Blog {BlogId} updated successfully", blog.BlogId);
                
                return RedirectToAction("Index", new { scrollTo = "BlogManagement" });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error updating blog {BlogId}", model.BlogId);
                TempData["ErrorMessage"] = $"Error updating blog: {ex.Message}";
                
                ViewBag.Categories = (await _unitOfWork.Categories.GetAllAsync())
                    .Select(c => new SelectListItem(c.Name, c.CategoryId.ToString()));
                ViewBag.Tags = (await _unitOfWork.Tags.GetAllAsync())
                    .Select(t => new SelectListItem(t.Name, t.TagId.ToString()));
                ViewBag.Quizzes = (await _unitOfWork.Quizzes.GetAllAsync())
                    .Select(q => new SelectListItem(q.Title, q.Id.ToString()));
                return View(model);
            }
        }

        [HttpPost("BlogDelete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BlogDelete(int id)
        {
            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var blog = await _unitOfWork.Blogs.GetAsync(id);
                if (blog == null)
                {
                    TempData["ErrorMessage"] = "Blog not found.";
                    return RedirectToAction("Index");
                }

                _logger.LogInformation("Deleting blog {BlogId} - {Title}", blog.BlogId, blog.Title);

                // Delete cover image
                if (!string.IsNullOrEmpty(blog.Image))
                {
                    await DeleteBlogFileAsync(blog.Image);
                }

                // Delete translations
                var translations = await _unitOfWork.BlogTranslations.GetAllAsync(bt => bt.BlogId == blog.BlogId);
                foreach (var translation in translations)
                {
                    _unitOfWork.BlogTranslations.Remove(translation);
                }

                // Remove blog
                _unitOfWork.Blogs.Remove(blog);
                await _unitOfWork.SaveAsync();
                await transaction.CommitAsync();

                TempData["SuccessMessage"] = $"Blog '{blog.Title}' deleted successfully!";
                _logger.LogInformation("Blog {BlogId} deleted successfully", blog.BlogId);
                
                return RedirectToAction("Index", new { scrollTo = "BlogManagement" });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error deleting blog {BlogId}", id);
                TempData["ErrorMessage"] = $"Error deleting blog: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        #endregion


        #region Helper Methods for SEO & Translations
        [HttpGet("GetQuizQuestions")]
        public async Task<IActionResult> GetQuizQuestions(int quizId)
        {
            if (quizId <= 0)
                return Json(new List<object>());

            var questions = await _unitOfWork.Questions.GetQuestionsByQuizIdAsync(quizId);
            var result = questions.Select(q => new
            {
                questionId = q.Id,
                questionText = q.QuestionText
            });
            return Json(result);
        }
        /// <summary>
        /// Generates a URL-friendly slug from text, supporting Turkish characters
        /// </summary>
        private string GenerateSlug(string text)
        {
            if (string.IsNullOrEmpty(text)) return "blog-post";
            
            // Convert to lowercase
            string slug = text.ToLowerInvariant();
            
            // Replace Turkish characters
            slug = slug
                .Replace("ç", "c")
                .Replace("ğ", "g")
                .Replace("ı", "i")
                .Replace("ö", "o")
                .Replace("ş", "s")
                .Replace("ü", "u")
                .Replace("İ", "i");
            
            // Replace spaces with hyphens
            slug = Regex.Replace(slug, @"\s+", "-");
            
            // Remove invalid characters (keep only alphanumeric and hyphens)
            slug = Regex.Replace(slug, @"[^a-z0-9\-]", "");
            
            // Remove multiple consecutive hyphens
            slug = Regex.Replace(slug, @"\-+", "-");
            
            // Trim hyphens from ends
            slug = slug.Trim('-');
            
            // Limit length
            if (slug.Length > 100)
            {
                slug = slug.Substring(0, 100).TrimEnd('-');
            }
            
            return string.IsNullOrEmpty(slug) ? "blog-post" : slug;
        }

        /// <summary>
        /// Ensures slug is unique by appending a number if necessary
        /// </summary>
        private async Task<string> EnsureUniqueSlugAsync(string slug, int? excludeBlogId = null)
        {
            var originalSlug = slug;
            var counter = 1;
            
            while (true)
            {
                var existingBlog = await _unitOfWork.Blogs
                    .GetAllAsync(b => b.Slug == slug);
                
                var exists = excludeBlogId.HasValue
                    ? existingBlog.Any(b => b.BlogId != excludeBlogId.Value)
                    : existingBlog.Any();
                
                if (!exists)
                {
                    return slug;
                }
                
                slug = $"{originalSlug}-{counter}";
                counter++;
            }
        }

        /// <summary>
        /// Saves blog translations to BlogTranslation table with SEO fields
        /// </summary>
        private async Task SaveBlogTranslationsAsync(int blogId, dynamic model)
        {
            // Turkish Translation
            if (!string.IsNullOrEmpty(model.TitleTR) || !string.IsNullOrEmpty(model.ContentTR))
            {
                var slugTR = !string.IsNullOrEmpty(model.SlugTR) 
                    ? GenerateSlug(model.SlugTR) 
                    : GenerateSlug(model.TitleTR ?? "");
                
                slugTR = await EnsureUniqueTranslationSlugAsync(slugTR, "tr", blogId);
                var imageTracker = new BlogImageTracker();
                var contentTR = await ProcessAndMoveImagesAsync(
                            model.ContentTR ?? "", 
                            blogId, 
                            imageTracker);
                var trTranslation = await _unitOfWork.BlogTranslations
                    .GetAllAsync(bt => bt.BlogId == blogId && bt.LanguageCode == "tr");
                var existingTR = trTranslation.FirstOrDefault();
                
                if (existingTR != null)
                {
                    // Update existing
                    existingTR.Title = model.TitleTR ?? string.Empty;
                    existingTR.Description = model.DescriptionTR ?? string.Empty;
                    existingTR.Content = contentTR;
                    existingTR.Slug = slugTR;
                    existingTR.MetaDescription = model.MetaDescriptionTR ?? string.Empty;
                    existingTR.MetaKeywords = model.MetaKeywordsTR ?? string.Empty;
                    existingTR.MetaTitle = model.MetaTitleTR ?? model.TitleTR ?? string.Empty;
                    existingTR.OgTitle = model.OgTitleTR ?? model.TitleTR ?? string.Empty;
                    existingTR.OgDescription = model.OgDescriptionTR ?? model.MetaDescriptionTR ?? string.Empty;
                    existingTR.LastModified = DateTime.UtcNow;
                    
                    _unitOfWork.BlogTranslations.Update(existingTR);
                }
                else
                {
                    // Create new
                    var newTR = new BlogTranslation
                    {
                        BlogId = blogId,
                        LanguageCode = "tr",
                        Title = model.TitleTR ?? string.Empty,
                        Description = model.DescriptionTR ?? string.Empty,
                        Content = contentTR,
                        Slug = slugTR,
                        MetaDescription = model.MetaDescriptionTR ?? string.Empty,
                        MetaKeywords = model.MetaKeywordsTR ?? string.Empty,
                        MetaTitle = model.MetaTitleTR ?? model.TitleTR ?? string.Empty,
                        OgTitle = model.OgTitleTR ?? model.TitleTR ?? string.Empty,
                        OgDescription = model.OgDescriptionTR ?? model.MetaDescriptionTR ?? string.Empty,
                        CreatedDate = DateTime.UtcNow
                    };
                    
                    await _unitOfWork.BlogTranslations.AddAsync(newTR);
                }
            }
            
            // German Translation
            if (!string.IsNullOrEmpty(model.TitleDE) || !string.IsNullOrEmpty(model.ContentDE))
            {
                var slugDE = !string.IsNullOrEmpty(model.SlugDE) 
                    ? GenerateSlug(model.SlugDE) 
                    : GenerateSlug(model.TitleDE ?? "");
                
                slugDE = await EnsureUniqueTranslationSlugAsync(slugDE, "de", blogId);
                var imageTracker = new BlogImageTracker();
                var contentDE = await ProcessAndMoveImagesAsync(
                    model.ContentDE ?? "", 
                    blogId, 
                    imageTracker);
                
                var deTranslations = await _unitOfWork.BlogTranslations
                    .GetAllAsync(bt => bt.BlogId == blogId && bt.LanguageCode == "de");
                var existingDE = deTranslations.FirstOrDefault();
                
                if (existingDE != null)
                {
                    // Update existing
                    existingDE.Title = model.TitleDE ?? string.Empty;
                    existingDE.Description = model.DescriptionDE ?? string.Empty;
                    existingDE.Content = contentDE;
                    existingDE.Slug = slugDE;
                    existingDE.MetaDescription = model.MetaDescriptionDE ?? string.Empty;
                    existingDE.MetaKeywords = model.MetaKeywordsDE ?? string.Empty;
                    existingDE.MetaTitle = model.MetaTitleDE ?? model.TitleDE ?? string.Empty;
                    existingDE.OgTitle = model.OgTitleDE ?? model.TitleDE ?? string.Empty;
                    existingDE.OgDescription = model.OgDescriptionDE ?? model.MetaDescriptionDE ?? string.Empty;
                    existingDE.LastModified = DateTime.UtcNow;
                    
                    _unitOfWork.BlogTranslations.Update(existingDE);
                }
                else
                {
                    // Create new
                    var newDE = new BlogTranslation
                    {
                        BlogId = blogId,
                        LanguageCode = "de",
                        Title = model.TitleDE ?? string.Empty,
                        Description = model.DescriptionDE ?? string.Empty,
                        Content = contentDE,
                        Slug = slugDE,
                        MetaDescription = model.MetaDescriptionDE ?? string.Empty,
                        MetaKeywords = model.MetaKeywordsDE ?? string.Empty,
                        MetaTitle = model.MetaTitleDE ?? model.TitleDE ?? string.Empty,
                        OgTitle = model.OgTitleDE ?? model.TitleDE ?? string.Empty,
                        OgDescription = model.OgDescriptionDE ?? model.MetaDescriptionDE ?? string.Empty,
                        CreatedDate = DateTime.UtcNow
                    };
                    
                    await _unitOfWork.BlogTranslations.AddAsync(newDE);
                }
            }
            
            await _unitOfWork.SaveAsync();
        }

        /// <summary>
        /// Ensures translation slug is unique within its language
        /// </summary>
        private async Task<string> EnsureUniqueTranslationSlugAsync(string slug, string languageCode, int blogId)
        {
            var originalSlug = slug;
            var counter = 1;
            
            while (true)
            {
                var existing = await _unitOfWork.BlogTranslations
                    .GetAllAsync(bt => bt.Slug == slug && bt.LanguageCode == languageCode && bt.BlogId != blogId);
                
                if (!existing.Any())
                {
                    return slug;
                }
                
                slug = $"{originalSlug}-{counter}";
                counter++;
            }
        }

        #endregion

        #region Image Upload & Management

        /// <summary>
        /// Handles file uploads from CKEditor. Files are temporarily stored with hash-based names
        /// to prevent duplicates and enable efficient tracking.
        /// </summary>
        [HttpPost("UploadFile")]
        public async Task<IActionResult> UploadFile(IFormFile upload, string blogId)
        {
            if (upload == null || upload.Length == 0)
            {
                return Json(new { uploaded = false, error = new { message = "No file uploaded." } });
            }

            try
            {
                // Validate file type
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                var extension = Path.GetExtension(upload.FileName).ToLower();
                
                if (!allowedExtensions.Contains(extension))
                {
                    return Json(new { uploaded = false, error = new { message = $"File type {extension} not allowed." } });
                }

                // Validate file size (5MB limit)
                const long maxSize = 5 * 1024 * 1024;
                if (upload.Length > maxSize)
                {
                    return Json(new { uploaded = false, error = new { message = "File size exceeds 5MB limit." } });
                }

                // Calculate file hash for deduplication
                string fileHash = await CalculateFileHashAsync(upload);
                string fileName = $"{fileHash}{extension}";
                
                // Save to temp directory
                string tempPath = Path.Combine(_env.WebRootPath, "temp");
                Directory.CreateDirectory(tempPath);
                
                string tempFilePath = Path.Combine(tempPath, fileName);

                // Only save if file doesn't already exist
                if (!System.IO.File.Exists(tempFilePath))
                {
                    using (var fileStream = new FileStream(tempFilePath, FileMode.Create))
                    {
                        await upload.CopyToAsync(fileStream);
                    }
                    _logger.LogDebug("Uploaded new temp file: {FileName} (Hash: {Hash})", fileName, fileHash);
                }
                else
                {
                    _logger.LogDebug("File already exists in temp: {FileName}", fileName);
                }

                // Return URL for CKEditor
                string tempUrl = $"/temp/{fileName}";
                return Json(new { uploaded = true, url = tempUrl });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading file");
                return Json(new { uploaded = false, error = new { message = "Upload failed: " + ex.Message } });
            }
        }

        /// <summary>
        /// Saves a blog cover image with hash-based deduplication
        /// </summary>
        private async Task<string?> SaveBlogCoverImageAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return null;

            try
            {
                // Validate
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                var extension = Path.GetExtension(file.FileName).ToLower();
                
                if (!allowedExtensions.Contains(extension))
                {
                    _logger.LogWarning("Invalid cover image extension: {Extension}", extension);
                    return null;
                }

                // Calculate hash
                string fileHash = await CalculateFileHashAsync(file);
                string fileName = $"{fileHash}{extension}";
                
                // Determine directory (gif vs img)
                string subDir = extension == ".gif" ? "gif" : "img";
                string dirPath = Path.Combine(_env.WebRootPath, "blog", subDir);
                Directory.CreateDirectory(dirPath);

                string filePath = Path.Combine(dirPath, fileName);
                string relativePath = $"/blog/{subDir}/{fileName}";

                // Save only if doesn't exist
                if (!System.IO.File.Exists(filePath))
                {
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    using (var input = file.OpenReadStream())
                    {
                        if (input.CanSeek)
                            input.Seek(0, SeekOrigin.Begin);
                        await input.CopyToAsync(fileStream);
                    }
                    _logger.LogInformation("Saved blog cover image: {Path}", relativePath);
                }
                else
                {
                    _logger.LogDebug("Cover image already exists: {Path}", relativePath);
                }

                return relativePath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving blog cover image");
                return null;
            }
        }

        /// <summary>
        /// Processes HTML content, moves temporary images to permanent storage,
        /// and tracks all images for future cleanup
        /// </summary>
        private async Task<string> ProcessAndMoveImagesAsync(string content, int blogId, BlogImageTracker tracker)
        {
            if (string.IsNullOrEmpty(content))
                return content;

            try
            {
                // Find all temp images in content
                var matches = Regex.Matches(content, @"src=[""'](?<url>/temp/[^""']+)[""']", RegexOptions.IgnoreCase);
                
                foreach (Match match in matches)
                {
                    string tempUrl = match.Groups["url"].Value;
                    string tempFilePath = Path.Combine(_env.WebRootPath, tempUrl.TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString()));

                    if (System.IO.File.Exists(tempFilePath))
                    {
                        string fileName = Path.GetFileName(tempFilePath);
                        string extension = Path.GetExtension(fileName).ToLower();
                        
                        // Determine target directory
                        string subDir = extension == ".gif" ? "gif" : "img";
                        string targetDir = Path.Combine(_env.WebRootPath, "blog", subDir);
                        Directory.CreateDirectory(targetDir);
                        
                        string targetPath = Path.Combine(targetDir, fileName);
                        string newUrl = $"/blog/{subDir}/{fileName}";

                        // Move or delete temp file
                        if (!System.IO.File.Exists(targetPath))
                        {
                            System.IO.File.Move(tempFilePath, targetPath);
                            _logger.LogDebug("Moved temp image {FileName} to permanent storage", fileName);
                        }
                        else
                        {
                            System.IO.File.Delete(tempFilePath);
                            _logger.LogDebug("Deleted duplicate temp image {FileName}", fileName);
                        }

                        // Replace URL in content
                        content = content.Replace(tempUrl, newUrl);
                        
                        // Track the image
                        tracker.AddImage(newUrl);
                    }
                    else
                    {
                        _logger.LogWarning("Temp image not found: {Path}", tempFilePath);
                    }
                }

                // Also track any existing blog images already in content (for edit scenarios)
                var existingMatches = Regex.Matches(content, @"src=[""'](?<url>/blog/(img|gif)/[^""']+)[""']", RegexOptions.IgnoreCase);
                foreach (Match match in existingMatches)
                {
                    tracker.AddImage(match.Groups["url"].Value);
                }

                return await Task.FromResult(content);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing images for blog {BlogId}", blogId);
                return await Task.FromResult(content);
            }
        }

        /// <summary>
        /// Deletes a blog file (image or other asset)
        /// </summary>
        private async Task DeleteBlogFileAsync(string relativePath)
        {
            if (string.IsNullOrEmpty(relativePath))
                return;

            await Task.Run(() =>
            {
                try
                {
                    string fullPath = Path.Combine(_env.WebRootPath, relativePath.TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString()));
                    
                    if (System.IO.File.Exists(fullPath))
                    {
                        System.IO.File.Delete(fullPath);
                        _logger.LogInformation("Deleted blog file: {Path}", relativePath);
                    }
                    else
                    {
                        _logger.LogDebug("File not found (may have been deleted already): {Path}", relativePath);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error deleting file: {Path}", relativePath);
                }
            });
        }

        /// <summary>
        /// Calculates MD5 hash of a file for deduplication
        /// </summary>
        private async Task<string> CalculateFileHashAsync(IFormFile file)
        {
            using var md5 = System.Security.Cryptography.MD5.Create();
            using var stream = file.OpenReadStream();
            var hash = await md5.ComputeHashAsync(stream);
            return BitConverter.ToString(hash).Replace("-", "").ToLower();
        }

        #endregion

        #region Image Tracking System

        /// <summary>
        /// Saves image tracking data to JSON file for a blog
        /// </summary>
        private async Task SaveImageTrackingAsync(BlogImageTracker tracker)
        {
            try
            {
                string trackingDir = Path.Combine(_env.WebRootPath, "blog", "tracking");
                Directory.CreateDirectory(trackingDir);
                
                string trackingFile = Path.Combine(trackingDir, $"blog_{tracker.BlogId}_{tracker.UrlSlug}.json");
                
                var json = System.Text.Json.JsonSerializer.Serialize(tracker, new System.Text.Json.JsonSerializerOptions 
                { 
                    WriteIndented = true 
                });
                
                await System.IO.File.WriteAllTextAsync(trackingFile, json);
                _logger.LogDebug("Saved image tracking for blog {BlogId}", tracker.BlogId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving image tracking for blog {BlogId}", tracker.BlogId);
            }
        }

        /// <summary>
        /// Loads image tracking data from JSON file
        /// </summary>
        private async Task<BlogImageTracker> LoadImageTrackerAsync(int blogId, string urlSlug)
        {
            try
            {
                string trackingDir = Path.Combine(_env.WebRootPath, "blog", "tracking");
                string trackingFile = Path.Combine(trackingDir, $"blog_{blogId}_{urlSlug}.json");
                
                if (System.IO.File.Exists(trackingFile))
                {
                    var json = await System.IO.File.ReadAllTextAsync(trackingFile);
                    var tracker = System.Text.Json.JsonSerializer.Deserialize<BlogImageTracker>(json);
                    if (tracker != null)
                    {
                        _logger.LogDebug("Loaded image tracking for blog {BlogId}", blogId);
                        return tracker;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading image tracking for blog {BlogId}", blogId);
            }
            
            // Return empty tracker if loading fails
            return new BlogImageTracker(blogId, urlSlug);
        }

        /// <summary>
        /// Deletes image tracking file
        /// </summary>
        private async Task DeleteImageTrackingAsync(int blogId, string urlSlug)
        {
            await Task.Run(() =>
            {
                try
                {
                    string trackingDir = Path.Combine(_env.WebRootPath, "blog", "tracking");
                    string trackingFile = Path.Combine(trackingDir, $"blog_{blogId}_{urlSlug}.json");
                    
                    if (System.IO.File.Exists(trackingFile))
                    {
                        System.IO.File.Delete(trackingFile);
                        _logger.LogInformation("Deleted image tracking for blog {BlogId}", blogId);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error deleting image tracking for blog {BlogId}", blogId);
                }
            });
        }

        #endregion

        #region Translation Management

        /// <summary>
        /// Saves blog translations to resource files
        /// </summary>
        private void SaveBlogTranslations(int blogId, string url, string titleEN, string contentEN, 
            string titleTR, string contentTR, string titleDE, string contentDE)
        {
            try
            {
                // English (default)
                _manageResourceService.AddOrUpdateResource($"Title_{blogId}_{url}_en", titleEN, "en-US");
                _manageResourceService.AddOrUpdateResource($"Content_{blogId}_{url}_en", contentEN, "en-US");

                // Turkish
                if (!string.IsNullOrEmpty(titleTR))
                    _manageResourceService.AddOrUpdateResource($"Title_{blogId}_{url}_tr", titleTR, "tr-TR");
                if (!string.IsNullOrEmpty(contentTR))
                    _manageResourceService.AddOrUpdateResource($"Content_{blogId}_{url}_tr", contentTR, "tr-TR");

                // German
                if (!string.IsNullOrEmpty(titleDE))
                    _manageResourceService.AddOrUpdateResource($"Title_{blogId}_{url}_de", titleDE, "de-DE");
                if (!string.IsNullOrEmpty(contentDE))
                    _manageResourceService.AddOrUpdateResource($"Content_{blogId}_{url}_de", contentDE, "de-DE");
                
                _logger.LogDebug("Saved translations for blog {BlogId}", blogId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving translations for blog {BlogId}", blogId);
            }
        }

        /// <summary>
        /// Deletes all translations for a blog
        /// </summary>
        private void DeleteBlogTranslations(int blogId, string url)
        {
            try
            {
                var cultures = new Dictionary<string, string>
                {
                    { "en-US", "en" },
                    { "tr-TR", "tr" },
                    { "de-DE", "de" }
                };

                foreach (var (culture, code) in cultures)
                {
                    string titleKey = $"Title_{blogId}_{url}_{code}";
                    string contentKey = $"Content_{blogId}_{url}_{code}";

                    _manageResourceService.DeleteResource(titleKey, culture);
                    _manageResourceService.DeleteResource(contentKey, culture);
                }
                
                _logger.LogDebug("Deleted translations for blog {BlogId}", blogId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting translations for blog {BlogId}", blogId);
            }
        }

        #endregion

        #region Cleanup Operations

        /// <summary>
        /// Cleans up temporary files older than 24 hours
        /// </summary>
        private async Task CleanupTempFilesAsync()
        {
            await Task.Run(() =>
            {
                try
                {
                    string tempPath = Path.Combine(_env.WebRootPath, "temp");
                    
                    if (!Directory.Exists(tempPath))
                        return;

                    var files = Directory.GetFiles(tempPath);
                    var cutoffTime = DateTime.Now.AddHours(-24);
                    int deletedCount = 0;

                    foreach (var file in files)
                    {
                        try
                        {
                            var fileInfo = new FileInfo(file);
                            if (fileInfo.LastWriteTime < cutoffTime)
                            {
                                System.IO.File.Delete(file);
                                deletedCount++;
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Could not delete temp file: {File}", file);
                        }
                    }

                    if (deletedCount > 0)
                    {
                        _logger.LogInformation("Cleaned up {Count} old temp files", deletedCount);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during temp file cleanup");
                }
            });
        }

        /// <summary>
        /// Performs a full audit of blog images and deletes orphaned files
        /// This should be run periodically (e.g., via scheduled job)
        /// </summary>
        [HttpPost("CleanupOrphanedBlogImages")]
        [Authorize(Roles = "Root,Admin")]
        public async Task<IActionResult> CleanupOrphanedBlogImages()
        {
            try
            {
                _logger.LogInformation("Starting orphaned image cleanup");
                
                string? imgDir = Path.Combine(_env.WebRootPath, "blog", "img");
                string? gifDir = Path.Combine(_env.WebRootPath, "blog", "gif");
                
                // Get all files
                var allFiles = new HashSet<string>();
                if (Directory.Exists(imgDir))
                    allFiles.UnionWith(Directory.GetFiles(imgDir)
                        .Select(Path.GetFileName)
                        .Where(fn => fn != null)
                        .Select(fn => fn!));
                if (Directory.Exists(gifDir))
                    allFiles.UnionWith(Directory.GetFiles(gifDir)
                        .Select(Path.GetFileName)
                        .Where(fn => fn != null)
                        .Select(fn => fn!));

                // Get all tracked images
                var trackedImages = new HashSet<string>();
                var blogs = await _unitOfWork.Blogs.GetAllAsync();
                
                foreach (var blog in blogs)
                {
                    var tracker = await LoadImageTrackerAsync(blog.BlogId, blog.Url);
                    foreach (var img in tracker.GetAllImages())
                    {
                        var fileName = Path.GetFileName(img);
                        trackedImages.Add(fileName);
                    }
                }

                // Find orphans
                var orphans = allFiles.Except(trackedImages).ToList();
                
                // Delete orphans
                int deletedCount = 0;
                foreach (var orphan in orphans)
                {
                    try
                    {
                        var imgPath = Path.Combine(imgDir, orphan);
                        var gifPath = Path.Combine(gifDir, orphan);
                        
                        if (System.IO.File.Exists(imgPath))
                        {
                            System.IO.File.Delete(imgPath);
                            deletedCount++;
                        }
                        else if (System.IO.File.Exists(gifPath))
                        {
                            System.IO.File.Delete(gifPath);
                            deletedCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Could not delete orphaned file: {File}", orphan);
                    }
                }

                TempData["SuccessMessage"] = $"Cleanup complete. Deleted {deletedCount} orphaned images.";
                _logger.LogInformation("Orphaned image cleanup complete. Deleted {Count} files", deletedCount);
                
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during orphaned image cleanup");
                TempData["ErrorMessage"] = "Error during cleanup: " + ex.Message;
                return RedirectToAction("Index");
            }
        }

        #endregion

        #region Migration Tools

        /// <summary>
        /// ONE-TIME MIGRATION: Generates tracking files for all existing blogs
        /// This extracts all images from existing blog content and creates tracking files
        /// </summary>
        [HttpPost("MigrateExistingBlogs")]
        [Authorize(Roles = "Root")]
        public async Task<IActionResult> MigrateExistingBlogs()
        {
            try
            {
                _logger.LogInformation("Starting blog migration to tracking system");
                
                var blogs = await _unitOfWork.Blogs.GetAllAsync();
                int successCount = 0;
                int errorCount = 0;
                var errors = new List<string>();

                foreach (var blog in blogs)
                {
                    try
                    {
                        _logger.LogInformation("Migrating blog {BlogId} - {Title}", blog.BlogId, blog.Title);
                        
                        var tracker = new BlogImageTracker(blog.BlogId, blog.Url);
                        
                        // Extract images from main content
                        ExtractImagesFromContent(blog.Content, tracker);
                        
                        // Extract images from translations
                        var contentTR = _dynamicResourceService.GetResource($"Content_{blog.BlogId}_{blog.Url}_tr", "tr-TR");
                        var contentDE = _dynamicResourceService.GetResource($"Content_{blog.BlogId}_{blog.Url}_de", "de-DE");
                        
                        if (!string.IsNullOrEmpty(contentTR))
                            ExtractImagesFromContent(contentTR, tracker);
                            
                        if (!string.IsNullOrEmpty(contentDE))
                            ExtractImagesFromContent(contentDE, tracker);
                        
                        // Save tracking file
                        await SaveImageTrackingAsync(tracker);
                        
                        successCount++;
                        _logger.LogInformation("✓ Successfully migrated blog {BlogId}", blog.BlogId);
                    }
                    catch (Exception ex)
                    {
                        errorCount++;
                        var errorMsg = $"Failed to migrate blog {blog.BlogId}: {ex.Message}";
                        errors.Add(errorMsg);
                        _logger.LogError(ex, "Error migrating blog {BlogId}", blog.BlogId);
                    }
                }

                var summary = new
                {
                    TotalBlogs = blogs.Count(),
                    Successful = successCount,
                    Failed = errorCount,
                    Errors = errors
                };

                TempData["SuccessMessage"] = $"Migration complete! {successCount} blogs migrated successfully. {errorCount} errors.";
                
                if (errors.Any())
                {
                    TempData["WarningMessage"] = "Some blogs failed migration. Check logs for details.";
                }
                
                _logger.LogInformation("Migration complete: {Summary}", System.Text.Json.JsonSerializer.Serialize(summary));
                
                return Json(summary);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fatal error during blog migration");
                TempData["ErrorMessage"] = "Migration failed: " + ex.Message;
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Helper method to extract image paths from HTML content
        /// </summary>
        private void ExtractImagesFromContent(string content, BlogImageTracker tracker)
        {
            if (string.IsNullOrEmpty(content))
                return;

            // Match all image sources in the content
            var matches = Regex.Matches(content, @"src=[""'](?<url>/blog/(img|gif)/[^""']+)[""']", RegexOptions.IgnoreCase);
            
            foreach (Match match in matches)
            {
                var imageUrl = match.Groups["url"].Value;
                tracker.AddImage(imageUrl);
                _logger.LogDebug("Found image: {ImageUrl}", imageUrl);
            }
        }

        /// <summary>
        /// Verifies tracking files for all blogs and reports any issues
        /// </summary>
        [HttpGet("VerifyBlogTracking")]
        [Authorize(Roles = "Root,Admin")]
        public async Task<IActionResult> VerifyBlogTracking()
        {
            try
            {
                var blogs = await _unitOfWork.Blogs.GetAllAsync();
                var report = new List<object>();
                int healthyCount = 0;
                int missingTrackingCount = 0;
                int issuesCount = 0;

                foreach (var blog in blogs)
                {
                    var status = new
                    {
                        BlogId = blog.BlogId,
                        Title = blog.Title,
                        Url = blog.Url,
                        HasTracking = false,
                        TrackedImageCount = 0,
                        ActualImageCount = 0,
                        Status = "Unknown",
                        Issues = new List<string>()
                    };

                    try
                    {
                        // Try to load tracking file
                        var tracker = await LoadImageTrackerAsync(blog.BlogId, blog.Url);
                        
                        if (tracker.Images.Any() || true) // Tracking file exists
                        {
                            var issues = new List<string>();
                            
                            // Count tracked images
                            int trackedCount = tracker.Images.Count;
                            
                            // Count actual images in content
                            int actualCount = 0;
                            actualCount += CountImagesInContent(blog.Content);
                            
                            var contentTR = _dynamicResourceService.GetResource($"Content_{blog.BlogId}_{blog.Url}_tr", "tr-TR");
                            var contentDE = _dynamicResourceService.GetResource($"Content_{blog.BlogId}_{blog.Url}_de", "de-DE");
                            actualCount += CountImagesInContent(contentTR);
                            actualCount += CountImagesInContent(contentDE);
                            
                            // Check for discrepancies
                            if (trackedCount != actualCount)
                            {
                                issues.Add($"Tracked images ({trackedCount}) doesn't match actual images ({actualCount})");
                            }
                            
                            // Verify each tracked image exists on disk
                            foreach (var img in tracker.Images)
                            {
                                var fullPath = Path.Combine(_env.WebRootPath, img.TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString()));
                                if (!System.IO.File.Exists(fullPath))
                                {
                                    issues.Add($"Tracked image missing from disk: {img}");
                                }
                            }
                            
                            string statusText = issues.Any() ? "⚠️ Issues" : "✓ Healthy";
                            
                            report.Add(new
                            {
                                status.BlogId,
                                status.Title,
                                status.Url,
                                HasTracking = true,
                                TrackedImageCount = trackedCount,
                                ActualImageCount = actualCount,
                                Status = statusText,
                                Issues = issues
                            });
                            
                            if (issues.Any())
                                issuesCount++;
                            else
                                healthyCount++;
                        }
                        else
                        {
                            report.Add(new
                            {
                                status.BlogId,
                                status.Title,
                                status.Url,
                                HasTracking = false,
                                TrackedImageCount = 0,
                                ActualImageCount = 0,
                                Status = "❌ No Tracking",
                                Issues = new[] { "No tracking file found" }
                            });
                            missingTrackingCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        report.Add(new
                        {
                            status.BlogId,
                            status.Title,
                            status.Url,
                            HasTracking = false,
                            TrackedImageCount = 0,
                            ActualImageCount = 0,
                            Status = "❌ Error",
                            Issues = new[] { $"Error: {ex.Message}" }
                        });
                        issuesCount++;
                    }
                }

                var summary = new
                {
                    TotalBlogs = blogs.Count(),
                    Healthy = healthyCount,
                    MissingTracking = missingTrackingCount,
                    WithIssues = issuesCount,
                    Details = report
                };

                _logger.LogInformation("Tracking verification complete: {Summary}", 
                    System.Text.Json.JsonSerializer.Serialize(new { summary.TotalBlogs, summary.Healthy, summary.MissingTracking, summary.WithIssues }));

                return Json(summary);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during tracking verification");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Helper to count images in HTML content
        /// </summary>
        private int CountImagesInContent(string content)
        {
            if (string.IsNullOrEmpty(content))
                return 0;
                
            var matches = Regex.Matches(content, @"src=[""'](?<url>/blog/(img|gif)/[^""']+)[""']", RegexOptions.IgnoreCase);
            return matches.Count;
        }

        /// <summary>
        /// Repairs tracking files for blogs with issues
        /// </summary>
        [HttpPost("RepairBlogTracking/{id}")]
        [Authorize(Roles = "Root")]
        public async Task<IActionResult> RepairBlogTracking(int id)
        {
            try
            {
                var blog = await _unitOfWork.Blogs.GetAsync(id);
                if (blog == null)
                {
                    return NotFound(new { error = "Blog not found" });
                }

                _logger.LogInformation("Repairing tracking for blog {BlogId} - {Title}", blog.BlogId, blog.Title);

                // Create new tracker
                var tracker = new BlogImageTracker(blog.BlogId, blog.Url);
                
                // Extract images from all content
                ExtractImagesFromContent(blog.Content, tracker);
                
                var contentTR = _dynamicResourceService.GetResource($"Content_{blog.BlogId}_{blog.Url}_tr", "tr-TR");
                var contentDE = _dynamicResourceService.GetResource($"Content_{blog.BlogId}_{blog.Url}_de", "de-DE");
                
                if (!string.IsNullOrEmpty(contentTR))
                    ExtractImagesFromContent(contentTR, tracker);
                    
                if (!string.IsNullOrEmpty(contentDE))
                    ExtractImagesFromContent(contentDE, tracker);
                
                // Save repaired tracker
                await SaveImageTrackingAsync(tracker);
                
                _logger.LogInformation("✓ Successfully repaired tracking for blog {BlogId}", blog.BlogId);
                
                return Ok(new
                {
                    BlogId = blog.BlogId,
                    Title = blog.Title,
                    TrackedImages = tracker.Images.Count,
                    Images = tracker.Images
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error repairing tracking for blog {BlogId}", id);
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Generates a comprehensive report of all blog images and their status
        /// </summary>
        [HttpGet("BlogImageReport")]
        [Authorize(Roles = "Root,Admin")]
        public async Task<IActionResult> BlogImageReport()
        {
            try
            {
                var report = new
                {
                    GeneratedAt = DateTime.UtcNow,
                    FileSystem = await GetFileSystemStats(),
                    Blogs = await GetBlogImageStats(),
                    OrphanedFiles = await FindOrphanedFiles()
                };

                return Json(report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating blog image report");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        private async Task<object> GetFileSystemStats()
        {
            return await Task.Run(() =>
            {
                var imgDir = Path.Combine(_env.WebRootPath, "blog", "img");
                var gifDir = Path.Combine(_env.WebRootPath, "blog", "gif");
                var tempDir = Path.Combine(_env.WebRootPath, "temp");

                long GetDirectorySize(string path)
                {
                    if (!Directory.Exists(path)) return 0;
                    return new DirectoryInfo(path).GetFiles("*", SearchOption.AllDirectories).Sum(f => f.Length);
                }

                int GetFileCount(string path)
                {
                    if (!Directory.Exists(path)) return 0;
                    return Directory.GetFiles(path).Length;
                }

                return new
                {
                    ImagesDirectory = new
                    {
                        Path = "/blog/img",
                        FileCount = GetFileCount(imgDir),
                        TotalSize = GetDirectorySize(imgDir),
                        TotalSizeMB = Math.Round(GetDirectorySize(imgDir) / 1024.0 / 1024.0, 2)
                    },
                    GifsDirectory = new
                    {
                        Path = "/blog/gif",
                        FileCount = GetFileCount(gifDir),
                        TotalSize = GetDirectorySize(gifDir),
                        TotalSizeMB = Math.Round(GetDirectorySize(gifDir) / 1024.0 / 1024.0, 2)
                    },
                    TempDirectory = new
                    {
                        Path = "/temp",
                        FileCount = GetFileCount(tempDir),
                        TotalSize = GetDirectorySize(tempDir),
                        TotalSizeMB = Math.Round(GetDirectorySize(tempDir) / 1024.0 / 1024.0, 2),
                        Warning = GetFileCount(tempDir) > 100 ? "⚠️ Too many temp files - cleanup recommended" : null
                    }
                };
            });
        }

        private async Task<object> GetBlogImageStats()
        {
            var blogs = await _unitOfWork.Blogs.GetAllAsync();
            var stats = new List<object>();

            foreach (var blog in blogs)
            {
                var tracker = await LoadImageTrackerAsync(blog.BlogId, blog.Url);
                stats.Add(new
                {
                    blog.BlogId,
                    blog.Title,
                    ImageCount = tracker.Images.Count,
                    HasCoverImage = !string.IsNullOrEmpty(blog.Image),
                    LastUpdated = tracker.LastUpdated
                });
            }

            return new
            {
                TotalBlogs = blogs.Count(),
                TotalTrackedImages = stats.Cast<dynamic>().Sum(s => s.ImageCount),
                BlogsWithoutImages = stats.Cast<dynamic>().Count(s => s.ImageCount == 0),
                Details = stats
            };
        }

        private async Task<object> FindOrphanedFiles()
        {
            return await Task.Run(async () =>
            {
                var imgDir = Path.Combine(_env.WebRootPath, "blog", "img");
                var gifDir = Path.Combine(_env.WebRootPath, "blog", "gif");

                var allFiles = new HashSet<string>();
                if (Directory.Exists(imgDir))
                    allFiles.UnionWith(Directory.GetFiles(imgDir)
                        .Select(Path.GetFileName)
                        .Where(fn => fn != null)
                        .Select(fn => fn!));
                if (Directory.Exists(gifDir))
                    allFiles.UnionWith(Directory.GetFiles(gifDir)
                        .Select(Path.GetFileName)
                        .Where(fn => fn != null)
                        .Select(fn => fn!));

                var trackedFiles = new HashSet<string>();
                var blogs = await _unitOfWork.Blogs.GetAllAsync();

                foreach (var blog in blogs)
                {
                    var tracker = await LoadImageTrackerAsync(blog.BlogId, blog.Url);
                    foreach (var img in tracker.Images)
                    {
                        trackedFiles.Add(Path.GetFileName(img));
                    }
                }

                var orphans = allFiles.Except(trackedFiles).ToList();

                return new
                {
                    Count = orphans.Count,
                    Files = orphans,
                    Warning = orphans.Count > 50 ? $"⚠️ {orphans.Count} orphaned files detected - cleanup recommended" : null
                };
            });
        }

        #endregion

        #region Word
        [HttpGet("WordCreate")]
        public async Task<IActionResult> WordCreate()
        {
            var quizzes = await _unitOfWork.Quizzes.GetAllAsync();
            var model = new WordCreateViewModel
            {
                Term = string.Empty,
                AvailableQuizzes = quizzes.Select(q => new SelectListItem
                {
                    Value = q.Id.ToString(),
                    Text = q.Title
                })
            };

            return View(model);
        }

        [HttpPost("WordCreate")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> WordCreate(WordCreateViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var word = new Word
                    {
                        Term = model.Term,
                        Definition = model.Definition,
                        Example = model.Example,
                        Pronunciation = model.Pronunciation,
                        Synonyms = model.Synonyms,
                        Origin = model.Origin,
                        IsFromApi = model.IsFromApi
                    };

                    if (model.SelectedQuizIds != null && model.SelectedQuizIds.Any())
                    {
                        var quizzes = await _unitOfWork.Quizzes.GetAllAsync();
                        var selectedQuizzesList = quizzes.Where(q => model.SelectedQuizIds.Contains(q.Id)).ToList();
                        word.Quizzes = selectedQuizzesList;
                    }

                    await _unitOfWork.Words.AddAsync(word);
                    await _unitOfWork.SaveAsync();

                    TempData["SuccessMessage"] = "Word created successfully!";
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error creating word: {ex.Message}");
                }
            }

            // Repopulate quizzes if validation fails
            model.AvailableQuizzes = (await _unitOfWork.Quizzes.GetAllAsync())
                .Select(q => new SelectListItem
                {
                    Value = q.Id.ToString(),
                    Text = q.Title
                });

            return View(model);
        }
        [Authorize(Roles = "Root,Admin,Teacher")]
        [HttpPost("WordDelete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> WordDelete(int id)
        {
            try
            {
                // Get the word with related quizzes to handle relationships
                var word = await _unitOfWork.Words.GetByIdAsync(id);

                if (word == null)
                {
                    TempData["ErrorMessage"] = "Word not found";
                    return RedirectToAction("Index");
                }

                // Remove any quiz relationships first
                if (word.Quizzes != null && word.Quizzes.Any())
                {
                    foreach (var quiz in word.Quizzes.ToList())
                    {
                        quiz.Words.Remove(word);
                    }
                }

                // Delete the word
                _unitOfWork.Words.Remove(word);
                await _unitOfWork.SaveAsync();

                TempData["SuccessMessage"] = $"Word '{word.Term}' deleted successfully";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error deleting word: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        [HttpGet("WordEdit/{id}")]
        public async Task<IActionResult> WordEdit(int id)
        {
            var word = await _unitOfWork.Words.GetByIdAsync(id);
            if (word == null)
            {
                return NotFound();
            }

            var quizzes = await _unitOfWork.Quizzes.GetAllAsync();

            var model = new WordEditViewModel
            {
                WordId = word.WordId,
                Term = word.Term,
                Definition = word.Definition,
                Example = word.Example,
                Pronunciation = word.Pronunciation,
                Synonyms = word.Synonyms,
                Origin = word.Origin,
                IsFromApi = word.IsFromApi,
                SelectedQuizIds = word.Quizzes?.Select(q => q.Id).ToList() ?? new List<int>(),
                AvailableQuizzes = quizzes.Select(q => new SelectListItem
                {
                    Value = q.Id.ToString(),
                    Text = q.Title
                })
            };

            return View(model);
        }

        [HttpPost("WordEdit/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> WordEdit(WordEditViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var word = await _unitOfWork.Words.GetByIdAsync(model.WordId);
                    if (word == null)
                    {
                        return NotFound();
                    }

                    // Update properties
                    word.Term = model.Term;
                    word.Definition = model.Definition;
                    word.Example = model.Example;
                    word.Pronunciation = model.Pronunciation;
                    word.Synonyms = model.Synonyms;
                    word.Origin = model.Origin;
                    word.IsFromApi = model.IsFromApi;

                    // Update quizzes
                    var Quizzes = await _unitOfWork.Quizzes
                        .GetAllAsync();
                    var selectedQuizzes = Quizzes
                        .Where(q => model.SelectedQuizIds.Contains(q.Id))
                        .ToList();
                    word.Quizzes.Clear();
                    foreach (var quiz in selectedQuizzes)
                    {
                        word.Quizzes.Add(quiz);
                    }

                    _unitOfWork.Words.Update(word);
                    await _unitOfWork.SaveAsync();

                    TempData["SuccessMessage"] = "Word updated successfully!";
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error updating word: {ex.Message}");
                }
            }

            // Repopulate if validation fails
            model.AvailableQuizzes = (await _unitOfWork.Quizzes.GetAllAsync())
                .Select(q => new SelectListItem
                {
                    Value = q.Id.ToString(),
                    Text = q.Title
                });

            return View(model);
        }

        #endregion

        #region Quiz
        [HttpPost("QuizDelete/{id}")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Root,Admin,Teacher")]
        public async Task<IActionResult> QuizDelete(int id, bool deleteQuestions = false)
        {
            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var quiz = await _unitOfWork.Quizzes.GetByIdAsync(id);
                if (quiz == null)
                {
                    TempData["ErrorMessage"] = "Quiz not found!";
                    return RedirectToAction("Index");
                }

                // Delete quiz media files (audio/video)
                DeleteQuizMediaFiles(quiz);

                // Remove related quiz submissions & responses
                var submissions = (await _unitOfWork.QuizSubmissions.GetAllAsync())
                    .Where(qs => qs.QuizId == id)
                    .ToList();
                foreach (var submission in submissions)
                {
                    // Remove responses first
                    foreach (var response in submission.QuizResponses.ToList())
                    {
                        _unitOfWork.QuizResponses.Remove(response);
                    }
                    _unitOfWork.QuizSubmissions.Remove(submission);
                }

                // Remove word and tag relationships if needed
                quiz.Words?.Clear();
                quiz.Tags?.Clear();

                if (deleteQuestions)
                {
                    // User chose to delete questions and their files
                    foreach (var question in quiz.Questions.ToList())
                    {
                        // Delete question media files (images, audio, videos)
                        DeleteQuestionMediaFiles(question);

                        // First, remove all answers associated with the question
                        foreach (var answer in question.Answers.ToList())
                        {
                            _unitOfWork.GenericRepository<Entity.QuizAnswer>().Remove(answer);
                        }
                        // Now, remove the question itself using the generic repository
                        _unitOfWork.GenericRepository<Entity.Question>().Remove(question);
                    }
                    _logger.LogInformation("Deleted quiz {QuizId} with {QuestionCount} questions and their files", id, quiz.Questions.Count);
                }
                else
                {
                    // User chose NOT to delete questions - unlink them from quiz
                    foreach (var question in quiz.Questions.ToList())
                    {
                        question.QuizId = 0;
                        question.Quiz = null;
                        _unitOfWork.GenericRepository<Entity.Question>().Update(question);
                    }
                    _logger.LogInformation("Deleted quiz {QuizId} but preserved {QuestionCount} questions", id, quiz.Questions.Count);
                }

                // Remove the quiz itself
                _unitOfWork.Quizzes.Remove(quiz);

                await _unitOfWork.SaveAsync();
                await transaction.CommitAsync();

                TempData["SuccessMessage"] = deleteQuestions 
                    ? "Quiz and associated questions deleted successfully!" 
                    : "Quiz deleted successfully! Questions have been preserved.";
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error deleting quiz {QuizId}", id);
                TempData["ErrorMessage"] = $"Error deleting quiz: {ex.Message}";
            }
            return RedirectToAction("Index", new { scrollTo = "QuizzesManagement" });
        }


        [HttpGet("QuizCreate")]
        public async Task<IActionResult> QuizCreate()
        {
            // Fetch only UNASSIGNED questions (not linked to any quiz) to prevent conflicts
            // Questions with QuizId will be excluded to avoid issues when editing/deleting quizzes
            var allQuestions = await _unitOfWork.Questions.GetAllAsync();
            var unassignedQuestions = allQuestions.Where(q => q.QuizId == 0 || q.Quiz == null);
            
            var availableQuestions = unassignedQuestions.Select(q => new QuestionViewModel
            {
                Id = q.Id,
                QuestionText = q.QuestionText,
                ImageUrl = q.ImageUrl,
                AudioUrl = q.AudioUrl,
                VideoUrl = q.VideoUrl,
                QuizTitle = "Available (Not assigned)",
                Answers = q.Answers?.Select(a => new AnswerViewModel
                {
                    Id = a.Id,
                    AnswerText = a.AnswerText,
                    IsCorrect = a.IsCorrect
                }).ToList() ?? new List<AnswerViewModel>()
            }).ToList();

            var model = new QuizCreateViewModel
            {
                Title = string.Empty,
                Description = string.Empty,
                Categories = (await _unitOfWork.Categories.GetAllAsync())
                    .Select(c => new SelectListItem(c.Name, c.CategoryId.ToString())),
                Tags = (await _unitOfWork.Tags.GetAllAsync())
                    .Select(t => new SelectListItem(t.Name, t.TagId.ToString())),
                Words = (await _unitOfWork.Words.GetAllAsync())
                    .Select(w => new SelectListItem(w.Term, w.WordId.ToString())),
                AvailableQuestions = availableQuestions
            };
            // Start with empty questions list (user can add from available or create new)
            model.Questions = new List<QuestionViewModel>();

            // Populate teacher selection options and default to current user
            // Use USERNAME as value instead of ID (since there are two separate databases)
            var currentUser = await _userManager.GetUserAsync(User);
            var allUsers = _userManager.Users.ToList();
            
            // Filter to only show users with Teacher, Admin, or Root roles
            var teacherUsers = new List<User>();
            foreach (var user in allUsers)
            {
                var roles = await _userManager.GetRolesAsync(user);
                if (roles.Any(r => r == "Root" || r == "Admin" || r == "Teacher"))
                {
                    teacherUsers.Add(user);
                }
            }
            
            model.TeacherOptions = teacherUsers.Select(u => new SelectListItem
            {
                Value = u.UserName ?? u.Id,  // Use Username as value
                Text = (!string.IsNullOrWhiteSpace(u.FirstName) || !string.IsNullOrWhiteSpace(u.LastName)) ?
                        string.Join(' ', new[] { u.FirstName, u.LastName }.Where(s => !string.IsNullOrWhiteSpace(s))) :
                        (u.UserName ?? u.Email ?? "(unknown)")
            }).ToList();
            if (currentUser != null)
            {
                // TeacherId holds the username for dropdown binding
                model.TeacherId = currentUser.UserName ?? currentUser.Id;
                
                // TeacherName is the display name (FirstName + LastName or fallback to Username/Email)
                model.TeacherName = string.Join(' ', new[] { currentUser.FirstName, currentUser.LastName }.Where(s => !string.IsNullOrWhiteSpace(s)));
                if (string.IsNullOrWhiteSpace(model.TeacherName)) 
                    model.TeacherName = currentUser.UserName ?? currentUser.Email ?? string.Empty;
            }

            return View(model);
        }

        [HttpPost("QuizCreate")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> QuizCreate(QuizCreateViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            // Determine the final TeacherName (username) to use
            string? finalTeacherName = null;
            
            if (model.IsAnonymous)
            {
                // Anonymous quiz - no teacher
                finalTeacherName = null;
            }
            else if (!string.IsNullOrWhiteSpace(model.TeacherId))
            {
                // Find teacher by USERNAME (TeacherId contains username from frontend)
                _logger.LogInformation("Searching for teacher with username: '{Username}'", model.TeacherId);
                var selectedTeacher = await _userManager.FindByNameAsync(model.TeacherId);
                if (selectedTeacher != null)
                {
                    finalTeacherName = selectedTeacher.UserName;
                    _logger.LogInformation("✓ Found teacher by username '{Username}'", model.TeacherId);
                }
                else
                {
                    // Teacher not found by username, use current user as fallback
                    _logger.LogWarning("✗ Teacher username '{Username}' NOT FOUND in database. Using current user '{Username}' as fallback.", model.TeacherId, user.UserName);
                    finalTeacherName = user.UserName;
                }
            }
            else
            {
                // No teacher selected, default to current user
                finalTeacherName = user.UserName;
            }

            if (ModelState.IsValid)
            {
                using var transaction = await _unitOfWork.BeginTransactionAsync();
                try
                {
                    // Handle Quiz Image Upload
                    var uploadedImageUrl = model.ImageFile != null ? await ProcessQuizImageUpload(model.ImageFile) : null;
                    
                    // Handle Quiz Audio Upload
                    var uploadedAudioUrl = model.AudioFile != null ? await ProcessQuizAudioUpload(model.AudioFile) : null;
                    
                    // Handle YouTube URL (text field)
                    var finalYouTubeUrl = !string.IsNullOrWhiteSpace(model.YouTubeVideoUrl) ? model.YouTubeVideoUrl.Trim() : null;
                    
                    var taglist = await _unitOfWork.Tags.GetAllAsync();
                    var wordlist = await _unitOfWork.Words.GetAllAsync();
                    
                    _logger.LogInformation("Creating quiz with TeacherName: '{TeacherName}' (IsAnonymous: {IsAnonymous})", 
                        finalTeacherName ?? "NULL", model.IsAnonymous);
                    
                    var quiz = new SpeakingClub.Entity.Quiz
                    {
                        Title = model.Title,
                        Description = model.Description,
                        TeacherName = finalTeacherName,
                        CategoryId = model.CategoryId,
                        ImageUrl = uploadedImageUrl,
                        AudioUrl = uploadedAudioUrl,
                        YouTubeVideoUrl = finalYouTubeUrl,
                        Tags = taglist.Where(t => model.SelectedTagIds.Contains(t.TagId)).ToList(),
                        Words = wordlist.Where(w => model.SelectedWordIds.Contains(w.WordId)).ToList()
                    };

                    foreach (var questionModel in model.Questions)
                    {
                        // Handle Question Image Upload
                        string? questionImageUrl = null;
                        if (questionModel.ImageFile != null && questionModel.ImageFile.Length > 0)
                        {
                            questionImageUrl = await ProcessQuizImageUpload(questionModel.ImageFile);
                        }
                        
                        // Handle Question Audio Upload
                        string? questionAudioUrl = null;
                        if (questionModel.AudioFile != null && questionModel.AudioFile.Length > 0)
                        {
                            questionAudioUrl = await ProcessQuizAudioUpload(questionModel.AudioFile);
                        }
                        
                        var question = new SpeakingClub.Entity.Question
                        {
                            QuestionText = questionModel.QuestionText,
                            ImageUrl = questionImageUrl,
                            AudioUrl = questionAudioUrl,
                            VideoUrl = !string.IsNullOrWhiteSpace(questionModel.VideoUrl) ? questionModel.VideoUrl.Trim() : null,
                            Answers = questionModel.Answers.Select(a => new SpeakingClub.Entity.QuizAnswer
                            {
                                AnswerText = a.AnswerText,
                                IsCorrect = a.IsCorrect
                            }).ToList()
                        };
                        quiz.Questions.Add(question);
                    }

                    await _unitOfWork.Quizzes.AddAsync(quiz);
                    await _unitOfWork.SaveAsync();
                    await transaction.CommitAsync();

                    TempData["SuccessMessage"] = "Quiz created successfully!";
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    ModelState.AddModelError("", $"Error creating quiz: {ex.Message}");
                }
            }

            // If ModelState is invalid, log ModelState errors and the incoming form keys/values to help diagnose binding issues
            if (!ModelState.IsValid)
            {
                try
                {
                    // Log ModelState errors
                    foreach (var kvp in ModelState)
                    {
                        var key = kvp.Key ?? "(null)";
                        var errors = kvp.Value.Errors.Select(e => e.ErrorMessage).Where(msg => !string.IsNullOrWhiteSpace(msg)).ToList();
                        if (errors.Count > 0)
                        {
                            _logger.LogWarning("ModelState error for key '{Key}': {Errors}", key, string.Join("; ", errors));
                        }
                    }

                    // Log Request.Form contents
                    if (Request?.HasFormContentType ?? false)
                    {
                        foreach (var formKey in Request.Form.Keys)
                        {
                            var values = Request.Form[formKey];
                            _logger.LogWarning("Form[{Key}] = {Values}", formKey, string.Join(",", values.ToArray()));
                        }
                    }
                    else
                    {
                        _logger.LogWarning("Request does not contain form data or HasFormContentType is false.");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error while logging ModelState/Form data for QuizCreate POST");
                }
            }

            // Repopulate dropdowns if validation fails
            model.Categories = (await _unitOfWork.Categories.GetAllAsync())
                .Select(c => new SelectListItem(c.Name, c.CategoryId.ToString()));
            model.Tags = (await _unitOfWork.Tags.GetAllAsync())
                .Select(t => new SelectListItem(t.Name, t.TagId.ToString()));
            model.Words = (await _unitOfWork.Words.GetAllAsync())
                .Select(w => new SelectListItem(w.Term, w.WordId.ToString()));
            
            // Repopulate teacher options using USERNAME as value
            // Filter to only show users with Teacher, Admin, or Root roles
            var allUsersForRepopulate = _userManager.Users.ToList();
            var teacherUsersForRepopulate = new List<User>();
            foreach (var usr in allUsersForRepopulate)
            {
                var roles = await _userManager.GetRolesAsync(usr);
                if (roles.Any(r => r == "Root" || r == "Admin" || r == "Teacher"))
                {
                    teacherUsersForRepopulate.Add(usr);
                }
            }
            
            model.TeacherOptions = teacherUsersForRepopulate.Select(u => new SelectListItem
            {
                Value = u.UserName ?? u.Id,
                Text = (!string.IsNullOrWhiteSpace(u.FirstName) || !string.IsNullOrWhiteSpace(u.LastName)) ?
                        string.Join(' ', new[] { u.FirstName, u.LastName }.Where(s => !string.IsNullOrWhiteSpace(s))) :
                        (u.UserName ?? u.Email ?? "(unknown)")
            }).ToList();

            return View(model);
        }

        [HttpGet("QuizEdit/{id:int}")]
        public async Task<IActionResult> QuizEdit(int id)
        {
            var quiz = await _unitOfWork.Quizzes.GetByIdAsync(id);

            if (quiz == null)
            {
                return NotFound();
            }

            var model = new QuizEditViewModel
            {
                QuizId = quiz.Id,
                Title = quiz.Title,
                Description = quiz.Description,
                CategoryId = quiz.CategoryId,
                SelectedTagIds = quiz.Tags.Select(t => t.TagId).ToList(),
                SelectedWordIds = quiz.Words.Select(w => w.WordId).ToList(),
                Questions = quiz.Questions.Select(q => new QuestionEditViewModel
                {
                    QuestionId = q.Id,
                    QuestionText = q.QuestionText,
                    ImageUrl = q.ImageUrl,
                    AudioUrl = q.AudioUrl,
                    VideoUrl = q.VideoUrl,
                    Answers = q.Answers.Select(a => new AnswerEditViewModel
                    {
                        AnswerId = a.Id,
                        AnswerText = a.AnswerText,
                        IsCorrect = a.IsCorrect.ToString().ToLower()
                    }).ToList()
                }).ToList(),
                // Quiz media fields (for display)
                ImageUrl = quiz.ImageUrl ?? string.Empty,
                AudioUrl = quiz.AudioUrl ?? string.Empty,
                YouTubeVideoUrl = quiz.YouTubeVideoUrl ?? string.Empty,
                // Teacher fields - TeacherId now holds username for frontend selection
                TeacherId = quiz.TeacherName,  // Store username in TeacherId for dropdown binding
                IsAnonymous = string.IsNullOrWhiteSpace(quiz.TeacherName),

                Categories = (await _unitOfWork.Categories.GetAllAsync())
                    .Select(c => new SelectListItem(c.Name, c.CategoryId.ToString())),
                Tags = (await _unitOfWork.Tags.GetAllAsync())
                    .Select(t => new SelectListItem(t.Name, t.TagId.ToString())),
                Words = (await _unitOfWork.Words.GetAllAsync())
                    .Select(w => new SelectListItem(w.Term, w.WordId.ToString()))
            };

            // Populate teacher selection options using USERNAME as value
            // Filter to only show users with Teacher, Admin, or Root roles
            var allUsers = _userManager.Users.ToList();
            var teacherUsers = new List<User>();
            foreach (var user in allUsers)
            {
                var roles = await _userManager.GetRolesAsync(user);
                if (roles.Any(r => r == "Root" || r == "Admin" || r == "Teacher"))
                {
                    teacherUsers.Add(user);
                }
            }
            
            model.TeacherOptions = teacherUsers.Select(u => new SelectListItem
            {
                Value = u.UserName ?? u.Id,  // Use username as value
                Text = (!string.IsNullOrWhiteSpace(u.FirstName) || !string.IsNullOrWhiteSpace(u.LastName)) ?
                        string.Join(' ', new[] { u.FirstName, u.LastName }.Where(s => !string.IsNullOrWhiteSpace(s))) :
                        (u.UserName ?? u.Email ?? "(unknown)")
            }).ToList();

            // Resolve teacher display name from username
            if (!string.IsNullOrWhiteSpace(quiz.TeacherName))
            {
                var teacher = await _userManager.FindByNameAsync(quiz.TeacherName);
                if (teacher != null)
                {
                    // Prefer FirstName + LastName if available otherwise Username/Email
                    var nameParts = new[] { teacher.FirstName, teacher.LastName }.Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();
                    model.TeacherName = nameParts.Length > 0 ? string.Join(' ', nameParts) : teacher.UserName ?? teacher.Email ?? "Unknown";
                }
                else
                {
                    model.TeacherName = "Unknown";
                }
            }
            else
            {
                model.TeacherName = "Anonymous";
            }

            return View(model);
        }

        [HttpPost("QuizEdit/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> QuizEdit(int id, QuizEditViewModel model)
        {
            if (id != model.QuizId)
            {
                return BadRequest("ID mismatch between route and form data.");
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            ValidateQuestions(model);

            if (!ModelState.IsValid)
            {
                model.Categories = await RepopulateCategories();
                model.Tags = await RepopulateTags();
                model.Words = await RepopulateWords();
                return View(model);
            }

            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var quiz = await _unitOfWork.Quizzes.GetByIdAsync(model.QuizId);
                if (quiz == null)
                {
                    return NotFound();
                }

                // Update basic properties
                quiz.Title = model.Title;
                quiz.Description = model.Description;
                quiz.CategoryId = model.CategoryId;

                // Handle Image Upload
                if (model.ImageFile != null && model.ImageFile.Length > 0)
                {
                    // Delete old image if exists
                    if (!string.IsNullOrEmpty(quiz.ImageUrl))
                    {
                        DeleteQuizMediaFile(quiz.ImageUrl);
                    }
                    // Upload new image
                    quiz.ImageUrl = await ProcessQuizImageUpload(model.ImageFile);
                }

                // Handle Audio Upload
                if (model.AudioFile != null && model.AudioFile.Length > 0)
                {
                    // Delete old audio if exists
                    if (!string.IsNullOrEmpty(quiz.AudioUrl))
                    {
                        DeleteQuizMediaFile(quiz.AudioUrl, "quiz audio");
                    }
                    // Upload new audio
                    quiz.AudioUrl = await ProcessQuizAudioUpload(model.AudioFile);
                }

                // Update YouTube URL (text field)
                quiz.YouTubeVideoUrl = string.IsNullOrWhiteSpace(model.YouTubeVideoUrl) 
                    ? null 
                    : model.YouTubeVideoUrl.Trim();

                // Update relationships
                await UpdateQuizRelationships(quiz, model);

                // Update questions and answers
                await UpdateQuestionsAndAnswers(quiz, model);

                // Handle teacher assignment - model.TeacherId contains username
                var originalTeacherName = quiz.TeacherName;
                bool teacherChanged = false;
                if (model.IsAnonymous)
                {
                    if (!string.IsNullOrWhiteSpace(originalTeacherName))
                    {
                        quiz.TeacherName = null;
                        teacherChanged = true;
                        _logger.LogInformation("Quiz {QuizId} set to anonymous", quiz.Id);
                    }
                }
                else
                {
                    // model.TeacherId contains the selected username
                    if (!string.IsNullOrWhiteSpace(model.TeacherId) && model.TeacherId != originalTeacherName)
                    {
                        var selectedTeacher = await _userManager.FindByNameAsync(model.TeacherId);
                        if (selectedTeacher != null)
                        {
                            quiz.TeacherName = selectedTeacher.UserName;
                            teacherChanged = true;
                            _logger.LogInformation("Quiz {QuizId} teacher changed to '{TeacherName}'", quiz.Id, selectedTeacher.UserName);
                        }
                    }
                }

                _unitOfWork.Quizzes.Update(quiz, modifyTeacherName: teacherChanged);
                await _unitOfWork.SaveAsync();
                await transaction.CommitAsync();

                TempData["SuccessMessage"] = "Quiz updated successfully!";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                ModelState.AddModelError("", $"Error updating quiz: {ex.Message}");
                model.Categories = await RepopulateCategories();
                model.Tags = await RepopulateTags();
                model.Words = await RepopulateWords();
                return View(model);
            }
        }

        #region helpers for quiz
        #region Quiz Media File Helpers

        /// <summary>
        /// Uploads a media file (image or audio) to the specified folder and returns the public URL path.
        /// </summary>
        /// <param name="file">The file to upload</param>
        /// <param name="subFolder">The subfolder name (e.g., "quiz-images", "quiz-audio")</param>
        /// <param name="allowedExtensions">Optional array of allowed file extensions</param>
        /// <param name="maxSizeInMB">Optional maximum file size in MB (default 10MB)</param>
        /// <returns>The public URL path to the uploaded file, or null if upload fails</returns>
        private async Task<string?> ProcessQuizMediaUpload(
            IFormFile? file, 
            string subFolder, 
            string[]? allowedExtensions = null,
            int maxSizeInMB = 10)
        {
            if (file == null || file.Length == 0)
                return null;

            // Default allowed extensions if not specified
            if (allowedExtensions == null)
            {
                allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".mp3", ".wav", ".ogg", ".m4a" };
            }

            // Validate file extension
            var fileExtension = Path.GetExtension(file.FileName).ToLower();
            if (!allowedExtensions.Contains(fileExtension))
            {
                _logger.LogWarning($"Invalid file extension: {fileExtension}. Allowed: {string.Join(", ", allowedExtensions)}");
                return null;
            }

            // Validate file size
            var maxSizeInBytes = maxSizeInMB * 1024 * 1024;
            if (file.Length > maxSizeInBytes)
            {
                _logger.LogWarning($"File size {file.Length} bytes exceeds maximum {maxSizeInBytes} bytes");
                return null;
            }

            try
            {
                // Create upload folder if it doesn't exist
                var uploadsFolder = Path.Combine(_env.WebRootPath, subFolder);
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                // Generate unique filename
                var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                // Save file to server
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(fileStream);
                }

                _logger.LogInformation($"Uploaded file: {filePath}");

                // Return public URL path
                return $"/{subFolder}/{uniqueFileName}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error uploading file to {subFolder}");
                return null;
            }
        }

        /// <summary>
        /// Uploads a quiz image file
        /// </summary>
        private async Task<string?> ProcessQuizImageUpload(IFormFile? imageFile)
        {
            return await ProcessQuizMediaUpload(
                imageFile, 
                "img/quiz-images",
                new[] { ".jpg", ".jpeg", ".png", ".gif" },
                maxSizeInMB: 5
            );
        }

        /// <summary>
        /// Uploads a quiz audio file
        /// </summary>
        private async Task<string?> ProcessQuizAudioUpload(IFormFile? audioFile)
        {
            return await ProcessQuizMediaUpload(
                audioFile,
                "mp3",
                new[] { ".mp3", ".wav", ".ogg", ".m4a" },
                maxSizeInMB: 10
            );
        }

        /// <summary>
        /// Deletes a media file from the server if it exists and is a local file (not external URL)
        /// </summary>
        /// <param name="fileUrl">The file URL path to delete (e.g., "/img/quiz-images/abc123.jpg")</param>
        /// <param name="fileType">Optional description of file type for logging purposes</param>
        private void DeleteQuizMediaFile(string? fileUrl, string fileType = "media file")
        {
            // Check if URL is null, empty, or an external URL
            if (string.IsNullOrEmpty(fileUrl) || fileUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation($"Skipping deletion for {fileType}: {fileUrl ?? "(null)"} (external or null)");
                return;
            }

            try
            {
                // Convert URL path to physical file path
                var filePath = Path.Combine(
                    _env.WebRootPath, 
                    fileUrl.TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString())
                );

                // Delete file if it exists
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                    _logger.LogInformation($"Deleted {fileType}: {filePath}");
                }
                else
                {
                    _logger.LogWarning($"{fileType} not found: {filePath}");
                }
            }
            catch (Exception ex)
            {
                // Log error but don't throw - we don't want file deletion errors to break the application
                _logger.LogError(ex, $"Error deleting {fileType}: {fileUrl}");
            }
        }

        /// <summary>
        /// Deletes all media files associated with a quiz (image, audio)
        /// </summary>
        private void DeleteQuizMediaFiles(Entity.Quiz quiz)
        {
            DeleteQuizMediaFile(quiz.ImageUrl, "quiz image");
            DeleteQuizMediaFile(quiz.AudioUrl, "quiz audio");
            // Note: YouTubeVideoUrl is just a URL, no file to delete
        }

        /// <summary>
        /// Deletes all media files associated with a question (image, audio)
        /// Note: VideoUrl is just a URL link, no file to delete
        /// </summary>
        private void DeleteQuestionMediaFiles(Entity.Question question)
        {
            DeleteQuizMediaFile(question.ImageUrl, "question image");
            DeleteQuizMediaFile(question.AudioUrl, "question audio");
            // Note: VideoUrl is just a URL (like YouTube link), no file to delete
        }

        #endregion
        private async Task UpdateQuizRelationships(Entity.Quiz quiz, QuizEditViewModel model)
        {
            // Update Tags
            var tags = await _unitOfWork.Tags.GetAllAsync();
            var selectedTags = tags.Where(t => model.SelectedTagIds.Contains(t.TagId));
            quiz.Tags = selectedTags.ToList();

            // Update Words
            var words = await _unitOfWork.Words.GetAllAsync();
            var selectedWords = words.Where(w => model.SelectedWordIds.Contains(w.WordId));
            quiz.Words = selectedWords.ToList();
        }

        private async Task UpdateQuestionsAndAnswers(SpeakingClub.Entity.Quiz quiz, QuizEditViewModel model)
        {
            // Define what constitutes an empty question
            bool IsEmptyQuestion(QuestionEditViewModel qm) =>
                string.IsNullOrWhiteSpace(qm.QuestionText)
                && qm.ImageFile == null
                && string.IsNullOrWhiteSpace(qm.VideoUrl)
                && (qm.Answers == null || qm.Answers.All(a => string.IsNullOrWhiteSpace(a.AnswerText)));

            // Get IDs of questions submitted from the form (only existing ones that are NOT empty)
            // This ensures deleted questions (submitted as empty) are excluded and will be removed
            var questionIdsFromModel = model.Questions
                .Where(q => q.QuestionId > 0 && !IsEmptyQuestion(q))
                .Select(q => q.QuestionId)
                .ToHashSet();

            // Identify and remove questions that were deleted from the UI
            var questionsToRemove = quiz.Questions.Where(q => !questionIdsFromModel.Contains(q.Id)).ToList();
            foreach (var question in questionsToRemove)
            {
                // Delete associated media files (images and audio)
                DeleteQuestionMediaFiles(question);

                // Remove from the parent collection so EF Core won't try to re-add it when saving
                quiz.Questions.Remove(question);
                // Also make sure any child answers are removed from the question collection to avoid tracked entities
                if (question.Answers != null && question.Answers.Any())
                {
                    foreach (var a in question.Answers.ToList())
                    {
                        // CRITICAL: Delete all QuizResponses that reference this answer BEFORE deleting the answer
                        var responsesToDelete = (await _unitOfWork.QuizResponses.GetAllAsync())
                            .Where(r => r.QuizAnswerId == a.Id)
                            .ToList();
                        
                        foreach (var response in responsesToDelete)
                        {
                            _unitOfWork.QuizResponses.Remove(response);
                        }

                        question.Answers.Remove(a);
                        _unitOfWork.GenericRepository<Entity.QuizAnswer>().Remove(a);
                    }
                }
                // Finally remove the question entity itself
                _unitOfWork.GenericRepository<Entity.Question>().Remove(question);
            }


            // Process submitted questions (update existing or add new)
            foreach (var questionModel in model.Questions)
            {
                // Skip fully empty question models (already handled in removal loop above)
                if (IsEmptyQuestion(questionModel))
                {
                    // Skip creating/updating this empty question
                    continue;
                }

                Entity.Question? question; // Make question nullable to handle potential null from FirstOrDefault

                if (questionModel.QuestionId > 0)
                {
                    // Find existing question
                    question = quiz.Questions.FirstOrDefault(q => q.Id == questionModel.QuestionId);
                    if (question == null) continue; // Skip if not found
                }
                else
                {
                    // Create a new question and add it to the quiz
                    question = new Entity.Question();
                    quiz.Questions.Add(question);
                }

                // Update question properties
                question.QuestionText = questionModel.QuestionText;
                question.VideoUrl = questionModel.VideoUrl;
                
                // Handle Question Image Upload
                if (questionModel.ImageFile != null && questionModel.ImageFile.Length > 0)
                {
                    // Delete old image if exists (for existing questions being updated)
                    if (questionModel.QuestionId > 0 && !string.IsNullOrEmpty(question.ImageUrl))
                    {
                        DeleteQuizMediaFile(question.ImageUrl, "question image");
                    }
                    question.ImageUrl = await ProcessQuizImageUpload(questionModel.ImageFile);
                }
                
                // Handle Question Audio Upload
                if (questionModel.AudioFile != null && questionModel.AudioFile.Length > 0)
                {
                    // Delete old audio if exists (for existing questions being updated)
                    if (questionModel.QuestionId > 0 && !string.IsNullOrEmpty(question.AudioUrl))
                    {
                        DeleteQuizMediaFile(question.AudioUrl, "question audio");
                    }
                    question.AudioUrl = await ProcessQuizAudioUpload(questionModel.AudioFile);
                }

                // --- Manage Answers for this Question ---
                var answerIdsFromModel = (questionModel.Answers ?? Enumerable.Empty<AnswerEditViewModel>())
                                            .Select(a => a.AnswerId)
                                            .Where(id => id > 0)
                                            .ToHashSet();

                // Identify and remove answers deleted from the UI OR those existing answers that were submitted empty
                var answersToRemove = (question.Answers ?? Enumerable.Empty<Entity.QuizAnswer>())
                    .Where(a =>
                        !answerIdsFromModel.Contains(a.Id) ||
                        // also remove if user submitted an existing answer but cleared its text
                        ((questionModel.Answers ?? Enumerable.Empty<AnswerEditViewModel>())
                            .Any(am => am.AnswerId == a.Id && string.IsNullOrWhiteSpace(am.AnswerText)))
                    ).ToList();

                foreach (var answer in answersToRemove)
                {
                    // CRITICAL: Delete all QuizResponses that reference this answer BEFORE deleting the answer
                    // This prevents foreign key constraint violations
                    var responsesToDelete = (await _unitOfWork.QuizResponses.GetAllAsync())
                        .Where(r => r.QuizAnswerId == answer.Id)
                        .ToList();
                    
                    foreach (var response in responsesToDelete)
                    {
                        _unitOfWork.QuizResponses.Remove(response);
                    }

                    // Remove from parent collection first to avoid reattachment by change tracker
                    if (question.Answers != null && question.Answers.Contains(answer))
                    {
                        question.Answers.Remove(answer);
                    }
                    // Directly remove from the context
                    _unitOfWork.GenericRepository<Entity.QuizAnswer>().Remove(answer);
                }

                // Process submitted answers (update existing or add new), but skip blank new answers
                foreach (var answerModel in questionModel.Answers ?? Enumerable.Empty<AnswerEditViewModel>())
                {
                    // If answer text is empty:
                    if (string.IsNullOrWhiteSpace(answerModel.AnswerText))
                    {
                        // If it was an existing answer, it has already been queued for removal above.
                        // Skip adding/updating blank answers.
                        continue;
                    }

                    Entity.QuizAnswer? answer;
                    if (answerModel.AnswerId > 0)
                    {
                        // question.Answers may be null for newly created questions; use null-conditional access
                        answer = question.Answers?.FirstOrDefault(a => a.Id == answerModel.AnswerId);
                        if (answer == null)
                        {
                            // This can happen if we removed it above; skip safely
                            continue;
                        }
                        // Update existing answer
                        answer.AnswerText = answerModel.AnswerText;
                        answer.IsCorrect = answerModel.IsCorrect == "true";
                    }
                    else
                    {
                        // Ensure the Answers collection is initialized before adding
                        if (question.Answers == null)
                        {
                            question.Answers = new System.Collections.Generic.List<Entity.QuizAnswer>();
                        }
                        // New answer with non-empty text -> add it
                        answer = new Entity.QuizAnswer
                        {
                            AnswerText = answerModel.AnswerText,
                            IsCorrect = answerModel.IsCorrect == "true"
                        };
                        question.Answers.Add(answer);
                    }
                }
            }
        }

        private void ValidateQuestions(QuizEditViewModel model)
        {
            // Helper to check if a question is empty
            bool IsEmptyQuestion(QuestionEditViewModel qm) =>
                string.IsNullOrWhiteSpace(qm.QuestionText)
                && qm.ImageFile == null
                && string.IsNullOrWhiteSpace(qm.VideoUrl)
                && (qm.Answers == null || qm.Answers.All(a => string.IsNullOrWhiteSpace(a.AnswerText)));

            for (int i = 0; i < model.Questions.Count; i++)
            {
                var question = model.Questions[i];
                
                // Skip validation for empty questions (they will be filtered out)
                if (IsEmptyQuestion(question))
                {
                    continue;
                }

                // Non-empty question must have question text
                if (string.IsNullOrWhiteSpace(question.QuestionText))
                {
                    ModelState.AddModelError($"Questions[{i}].QuestionText", "Question text is required for non-empty questions.");
                }

                // Check that at least one answer has text
                bool hasAnyAnswerText = question.Answers != null && question.Answers.Any(a => !string.IsNullOrWhiteSpace(a.AnswerText));
                if (!hasAnyAnswerText)
                {
                    ModelState.AddModelError($"Questions[{i}].Answers", "At least one answer is required for each question.");
                }

                // Validate individual answers that are not empty
                if (question.Answers != null)
                {
                    for (int j = 0; j < question.Answers.Count; j++)
                    {
                        var answer = question.Answers[j];
                        // If answer has an ID (existing) or other data, it should have text
                        if (answer.AnswerId > 0 && string.IsNullOrWhiteSpace(answer.AnswerText))
                        {
                            // This will be handled by removal logic, so skip
                            continue;
                        }
                    }
                }

                // Check that at least one answer is marked as correct (if there are any non-empty answers)
                if (hasAnyAnswerText)
                {
                    bool hasCorrectAnswer = question.Answers != null && 
                                          question.Answers.Any(a => !string.IsNullOrWhiteSpace(a.AnswerText) && a.IsCorrect == "true");
                    if (!hasCorrectAnswer)
                    {
                        ModelState.AddModelError($"Questions[{i}].Answers", "At least one answer must be marked as correct.");
                    }
                }
            }
        }

        private async Task<IEnumerable<SelectListItem>> RepopulateCategories()
        {
            return (await _unitOfWork.Categories.GetAllAsync())
                .Select(c => new SelectListItem(c.Name, c.CategoryId.ToString()));
        }

        private async Task<IEnumerable<SelectListItem>> RepopulateTags()
        {
            return (await _unitOfWork.Tags.GetAllAsync())
                .Select(t => new SelectListItem(t.Name, t.TagId.ToString()));
        }

        private async Task<IEnumerable<SelectListItem>> RepopulateWords()
        {
            return (await _unitOfWork.Words.GetAllAsync())
                .Select(w => new SelectListItem(w.Term, w.WordId.ToString()));
        }
        #endregion
        #endregion

        #region Slides
        #region Caraousel
        [HttpGet("CarouselCreate")]
        public IActionResult CarouselCreate()
        {
            return View(new CarouselViewModel());
        }


        [HttpPost("CarouselCreate")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CarouselCreate(CarouselViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Please fix the validation errors.";
                return View(model);
            }

            // Validate that at least one image is provided
            if (model.CarouselImage == null)
            {
                ModelState.AddModelError("CarouselImage", "Primary image is required.");
                return View(model);
            }

            var carousel = new Entity.SlideShow
            {
                CarouselTitle = model.CarouselTitle,
                CarouselDescription = model.CarouselDescription,
                CarouselLink = model.CarouselLink,
                CarouselLinkText = model.CarouselLinkText,
                DateAdded = DateTime.UtcNow,
            };

            try
            {
                // Validate and upload images
                carousel.CarouselImage = await ValidateAndUploadCarouselImage(model.CarouselImage, "Primary");
                if (carousel.CarouselImage == null)
                    return View(model);

                carousel.CarouselImage600w = model.CarouselImage600w != null 
                    ? await ValidateAndUploadCarouselImage(model.CarouselImage600w, "600w") ?? string.Empty
                    : string.Empty;
                    
                carousel.CarouselImage1200w = model.CarouselImage1200w != null 
                    ? await ValidateAndUploadCarouselImage(model.CarouselImage1200w, "1200w") ?? string.Empty
                    : string.Empty;

                // Save to database and retrieve entity with assigned ID
                carousel = await _unitOfWork.Slides.CreateAndReturn(carousel);
                await _unitOfWork.SaveAsync();
                
                int carouselId = carousel.SlideId;

                if (carouselId <= 0)
                {
                    TempData["ErrorMessage"] = "An error occurred while creating the carousel.";
                    return View(model);
                }

                // Save translations using the valid ID
                SaveAllTranslations(_manageResourceService, carouselId, model);

                TempData["SuccessMessage"] = "Carousel created successfully!";
                return RedirectToAction("Index", new { scrollTo = "SlideListManagement" });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to create carousel. Exception: {ex.Message}");
                TempData["ErrorMessage"] = "An error occurred while creating the carousel.";
                return View(model);
            }
        }



        [HttpGet("CarouselEdit/{id}")]
        public async Task<IActionResult> CarouselEdit(int id)
        {
            Console.WriteLine($"[INFO] Loading edit view for Carousel ID: {id}");

            // Fetch carousel from the database
            var carousel = await _unitOfWork.Slides.GetByIdAsync(id);
            if (carousel == null)
            {
                Console.WriteLine($"[WARN] Carousel with ID {id} not found.");
                return NotFound();
            }

            // Fetch translations with null checks and default values
            string GetTranslation(string key, string culture, string defaultValue)
            {
                var value = _manageResourceService.Read(key, culture);
                if (string.IsNullOrEmpty(value))
                {
                    Console.WriteLine($"[WARN] Resource key '{key}' not found in '{culture}' resource file.");
                    return defaultValue; // Provide a fallback value if the key is missing
                }
                return value;
            }

            // Create model
            var model = new CarouselEditModel
            {
                CarouselId = carousel.SlideId,
                CarouselTitle = carousel.CarouselTitle,
                CarouselDescription = carousel.CarouselDescription,
                CarouselLink = carousel.CarouselLink,
                CarouselLinkText = carousel.CarouselLinkText,
                DateAdded = carousel.DateAdded,
                CarouselImagePath = carousel.CarouselImage,
                CarouselImage1200wPath = carousel.CarouselImage1200w,
                CarouselImage600wPath = carousel.CarouselImage600w,
                // TR Translations
                CarouselTitleTR = GetTranslation($"Carousel_{carousel.SlideId}_Title", "tr-TR", carousel.CarouselTitle),
                CarouselDescriptionTR = GetTranslation($"Carousel_{carousel.SlideId}_Description", "tr-TR", carousel.CarouselDescription),
                CarouselLinkTextTR = GetTranslation($"Carousel_{carousel.SlideId}_LinkText", "tr-TR", carousel.CarouselLinkText),

                // DE Translations
                CarouselTitleDE = GetTranslation($"Carousel_{carousel.SlideId}_Title", "de-DE", carousel.CarouselTitle),
                CarouselDescriptionDE = GetTranslation($"Carousel_{carousel.SlideId}_Description", "de-DE", carousel.CarouselDescription),
                CarouselLinkTextDE = GetTranslation($"Carousel_{carousel.SlideId}_LinkText", "de-DE", carousel.CarouselLinkText)
            };

            Console.WriteLine("[INFO] Loaded carousel and translations successfully.");
            return View(model);
        }




        [HttpPost("CarouselEdit/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CarouselEdit(int id, CarouselEditModel model)
        {
            if (id != model.CarouselId)
                return BadRequest();

            if (!ModelState.IsValid)
            {
                // Reload existing paths for the view
                var existingCarousel = await _unitOfWork.Slides.GetByIdAsync(model.CarouselId);
                if (existingCarousel != null)
                {
                    model.CarouselImagePath = existingCarousel.CarouselImage;
                    model.CarouselImage600wPath = existingCarousel.CarouselImage600w;
                    model.CarouselImage1200wPath = existingCarousel.CarouselImage1200w;
                }
                return View(model);
            }

            var carousel = await _unitOfWork.Slides.GetByIdAsync(model.CarouselId);
            if (carousel == null)
            {
                TempData["ErrorMessage"] = "Carousel not found.";
                return RedirectToAction("Index", new { scrollTo = "SlideListManagement" });
            }

            try
            {
                // Log the incoming model data
                _logger.LogInformation($"[CarouselEdit] Updating carousel ID: {carousel.SlideId}");
                _logger.LogInformation($"[CarouselEdit] Model.CarouselLink: '{model.CarouselLink}'");
                _logger.LogInformation($"[CarouselEdit] Model.CarouselTitle: '{model.CarouselTitle}'");
                
                // Update carousel properties
                carousel.CarouselTitle = model.CarouselTitle;
                carousel.CarouselDescription = model.CarouselDescription;
                carousel.CarouselLink = model.CarouselLink;
                carousel.CarouselLinkText = model.CarouselLinkText;
                
                // Log after assignment
                _logger.LogInformation($"[CarouselEdit] After assignment - carousel.CarouselLink: '{carousel.CarouselLink}'");

                // Update images only if new ones are uploaded
                if (model.CarouselImage != null)
                {
                    var newImage = await ValidateAndUploadCarouselImage(model.CarouselImage, "Primary");
                    if (newImage != null)
                    {
                        // Delete old image
                        DeleteFile(carousel.CarouselImage, "CarouselImage");
                        carousel.CarouselImage = newImage;
                    }
                }

                if (model.CarouselImage600w != null)
                {
                    var newImage = await ValidateAndUploadCarouselImage(model.CarouselImage600w, "600w");
                    if (newImage != null)
                    {
                        DeleteFile(carousel.CarouselImage600w, "CarouselImage600w");
                        carousel.CarouselImage600w = newImage;
                    }
                }

                if (model.CarouselImage1200w != null)
                {
                    var newImage = await ValidateAndUploadCarouselImage(model.CarouselImage1200w, "1200w");
                    if (newImage != null)
                    {
                        DeleteFile(carousel.CarouselImage1200w, "CarouselImage1200w");
                        carousel.CarouselImage1200w = newImage;
                    }
                }

                // Update translations
                UpdateCarouselTranslations(carousel.SlideId, model);

                // Log before save
                _logger.LogInformation($"[CarouselEdit] Before UpdateAsync - carousel.CarouselLink: '{carousel.CarouselLink}'");
                
                // Save carousel
                await _unitOfWork.Slides.UpdateAsync(carousel);
                
                _logger.LogInformation($"[CarouselEdit] After UpdateAsync - carousel.CarouselLink: '{carousel.CarouselLink}'");
                
                await _unitOfWork.SaveAsync();
                
                _logger.LogInformation($"[CarouselEdit] After SaveAsync - Changes committed to database");
                
                TempData["SuccessMessage"] = "Carousel updated successfully!";
                return RedirectToAction("Index", new { scrollTo = "SlideListManagement" });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to update carousel. Exception: {ex.Message}");
                TempData["ErrorMessage"] = "An error occurred while updating the carousel.";
                return View(model);
            }
        }


        [HttpPost("CarouselDelete/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CarouselDelete(int id)
        {
            var carousel = await _unitOfWork.Slides.GetByIdAsync(id);
            if (carousel == null)
            {
                TempData["ErrorMessage"] = "Carousel not found.";
                return RedirectToAction("Index", new { scrollTo = "SlideListManagement" });
            }

            try
            {
                // Delete associated image files
                DeleteFile(carousel.CarouselImage, "CarouselImage");
                DeleteFile(carousel.CarouselImage600w, "CarouselImage600w");
                DeleteFile(carousel.CarouselImage1200w, "CarouselImage1200w");

                // Delete translations
                DeleteCarouselTranslations(carousel.SlideId);

                // Delete the carousel from the database
                await _unitOfWork.Slides.DeleteAsync(id);
                await _unitOfWork.SaveAsync();

                TempData["SuccessMessage"] = "Carousel deleted successfully!";
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to delete carousel with ID {id}. Exception: {ex.Message}");
                TempData["ErrorMessage"] = "An error occurred while deleting the carousel.";
            }

            return RedirectToAction("Index", new { scrollTo = "SlideListManagement" });
        }

        #region Carousel Helper Methods
        
        private async Task<string?> ValidateAndUploadCarouselImage(IFormFile file, string imageName)
        {
            if (file == null || file.Length == 0)
                return null;

            // Validate file extension
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(fileExtension))
            {
                ModelState.AddModelError("", $"{imageName} image must be a valid image format (jpg, jpeg, png, gif, webp).");
                return null;
            }

            // Validate file size (max 5MB)
            if (file.Length > 5 * 1024 * 1024)
            {
                ModelState.AddModelError("", $"{imageName} image must be less than 5MB.");
                return null;
            }

            return await SaveFile(file);
        }

        private void UpdateCarouselTranslations(int carouselId, CarouselEditModel model)
        {
            var baseKey = $"Carousel_{carouselId}";

            // Turkish translations
            if (!string.IsNullOrWhiteSpace(model.CarouselTitleTR))
                _manageResourceService.AddOrUpdate($"{baseKey}_Title", model.CarouselTitleTR, "tr-TR");
            if (!string.IsNullOrWhiteSpace(model.CarouselDescriptionTR))
                _manageResourceService.AddOrUpdate($"{baseKey}_Description", model.CarouselDescriptionTR, "tr-TR");
            if (!string.IsNullOrWhiteSpace(model.CarouselLinkTextTR))
                _manageResourceService.AddOrUpdate($"{baseKey}_LinkText", model.CarouselLinkTextTR, "tr-TR");

            // German translations
            if (!string.IsNullOrWhiteSpace(model.CarouselTitleDE))
                _manageResourceService.AddOrUpdate($"{baseKey}_Title", model.CarouselTitleDE, "de-DE");
            if (!string.IsNullOrWhiteSpace(model.CarouselDescriptionDE))
                _manageResourceService.AddOrUpdate($"{baseKey}_Description", model.CarouselDescriptionDE, "de-DE");
            if (!string.IsNullOrWhiteSpace(model.CarouselLinkTextDE))
                _manageResourceService.AddOrUpdate($"{baseKey}_LinkText", model.CarouselLinkTextDE, "de-DE");
        }

        private void DeleteCarouselTranslations(int carouselId)
        {
            var baseKey = $"Carousel_{carouselId}";
            string[] cultures = { "tr-TR", "de-DE" };
            string[] keys = { "Title", "Description", "LinkText" };

            foreach (var culture in cultures)
            {
                foreach (var key in keys)
                {
                    string resourceKey = $"{baseKey}_{key}";
                    _manageResourceService.Delete(resourceKey, culture);
                }
            }
        }

        #endregion

        #endregion
        #endregion

        #region Question Management
        // GET: Question Create
        [HttpGet("QuestionCreate")]
        public async Task<IActionResult> QuestionCreate()
        {
            var quizzes = await _unitOfWork.Quizzes.GetAllAsync();
            var model = new QuestionEditViewModel
            {
                QuestionId = 0,
                AvailableQuizzes = quizzes.Select(q => new SelectListItem
                {
                    Value = q.Id.ToString(),
                    Text = q.Title
                }),
                Answers = new List<AnswerEditViewModel>
                {
                    new AnswerEditViewModel { AnswerId = 0, AnswerText = "", IsCorrect = "false" }
                }
            };
            return View("QuestionCreateEdit", model);
        }

        // POST: Question Create
        [HttpPost("QuestionCreate")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> QuestionCreate(QuestionEditViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var quizzes = await _unitOfWork.Quizzes.GetAllAsync();
                model.AvailableQuizzes = quizzes.Select(q => new SelectListItem
                {
                    Value = q.Id.ToString(),
                    Text = q.Title
                });
                return View("QuestionCreateEdit", model);
            }

            // Check if at least one answer is correct
            if (!model.Answers.Any(a => a.IsCorrect == "true"))
            {
                ModelState.AddModelError("", "At least one answer must be marked as correct.");
                var quizzes = await _unitOfWork.Quizzes.GetAllAsync();
                model.AvailableQuizzes = quizzes.Select(q => new SelectListItem
                {
                    Value = q.Id.ToString(),
                    Text = q.Title
                });
                return View("QuestionCreateEdit", model);
            }

            // Upload media files if provided
            string? imageUrl = null;
            string? audioUrl = null;

            if (model.ImageFile != null)
            {
                imageUrl = await ProcessQuizImageUpload(model.ImageFile);
            }

            if (model.AudioFile != null)
            {
                audioUrl = await ProcessQuizAudioUpload(model.AudioFile);
            }

            // Create the question entity
            var question = new Entity.Question
            {
                QuestionText = model.QuestionText ?? string.Empty,
                ImageUrl = imageUrl,
                AudioUrl = audioUrl,
                VideoUrl = model.VideoUrl,
                QuizId = model.QuizId,
                Answers = model.Answers.Select(a => new Entity.QuizAnswer
                {
                    AnswerText = a.AnswerText,
                    IsCorrect = a.IsCorrect == "true"
                }).ToList()
            };

            await _unitOfWork.Questions.AddAsync(question);
            TempData["SuccessMessage"] = "Question created successfully!";
            return RedirectToAction("Index", new { scrollTo = "QuestionsManagement" });
        }

        // GET: Question Edit
        [HttpGet("QuestionEdit/{id}")]
        public async Task<IActionResult> QuestionEdit(int id)
        {
            var question = await _unitOfWork.Questions.GetByIdAsync(id);
            if (question == null)
            {
                return NotFound();
            }

            var quizzes = await _unitOfWork.Quizzes.GetAllAsync();
            var model = new QuestionEditViewModel
            {
                QuestionId = question.Id,
                QuestionText = question.QuestionText,
                ImageUrl = question.ImageUrl,
                AudioUrl = question.AudioUrl,
                VideoUrl = question.VideoUrl,
                QuizId = question.QuizId,
                AvailableQuizzes = quizzes.Select(q => new SelectListItem
                {
                    Value = q.Id.ToString(),
                    Text = q.Title,
                    Selected = q.Id == question.QuizId
                }),
                Answers = question.Answers.Select(a => new AnswerEditViewModel
                {
                    AnswerId = a.Id,
                    AnswerText = a.AnswerText,
                    IsCorrect = a.IsCorrect ? "true" : "false"
                }).ToList()
            };

            return View("QuestionCreateEdit", model);
        }

        // POST: Question Edit
        [HttpPost("QuestionEdit/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> QuestionEdit(int id, QuestionEditViewModel model)
        {
            if (id != model.QuestionId)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                var quizzes = await _unitOfWork.Quizzes.GetAllAsync();
                model.AvailableQuizzes = quizzes.Select(q => new SelectListItem
                {
                    Value = q.Id.ToString(),
                    Text = q.Title
                });
                return View("QuestionCreateEdit", model);
            }

            // Check if at least one answer is correct
            if (!model.Answers.Any(a => a.IsCorrect == "true"))
            {
                ModelState.AddModelError("", "At least one answer must be marked as correct.");
                var quizzes = await _unitOfWork.Quizzes.GetAllAsync();
                model.AvailableQuizzes = quizzes.Select(q => new SelectListItem
                {
                    Value = q.Id.ToString(),
                    Text = q.Title
                });
                return View("QuestionCreateEdit", model);
            }

            var question = await _unitOfWork.Questions.GetByIdAsync(id);
            if (question == null)
            {
                return NotFound();
            }

            // Upload new media files if provided
            if (model.ImageFile != null)
            {
                // Delete old image if exists
                if (!string.IsNullOrEmpty(question.ImageUrl))
                {
                    DeleteQuizMediaFile(question.ImageUrl);
                }
                question.ImageUrl = await ProcessQuizImageUpload(model.ImageFile);
            }

            if (model.AudioFile != null)
            {
                // Delete old audio if exists
                if (!string.IsNullOrEmpty(question.AudioUrl))
                {
                    DeleteQuizMediaFile(question.AudioUrl);
                }
                question.AudioUrl = await ProcessQuizAudioUpload(model.AudioFile);
            }

            // Update question properties
            question.QuestionText = model.QuestionText ?? string.Empty;
            question.VideoUrl = model.VideoUrl;
            question.QuizId = model.QuizId;

            // Update answers
            // Remove old answers that are not in the model
            var answersToRemove = question.Answers
                .Where(a => !model.Answers.Any(ma => ma.AnswerId == a.Id))
                .ToList();

            foreach (var answer in answersToRemove)
            {
                question.Answers.Remove(answer);
            }

            // Update or add answers
            foreach (var answerModel in model.Answers)
            {
                var existingAnswer = question.Answers.FirstOrDefault(a => a.Id == answerModel.AnswerId);
                if (existingAnswer != null)
                {
                    // Update existing answer
                    existingAnswer.AnswerText = answerModel.AnswerText;
                    existingAnswer.IsCorrect = answerModel.IsCorrect == "true";
                }
                else
                {
                    // Add new answer
                    question.Answers.Add(new Entity.QuizAnswer
                    {
                        AnswerText = answerModel.AnswerText,
                        IsCorrect = answerModel.IsCorrect == "true",
                        QuestionId = question.Id
                    });
                }
            }

            _unitOfWork.Questions.Update(question);
            TempData["SuccessMessage"] = "Question updated successfully!";
            return RedirectToAction("Index", new { scrollTo = "QuestionsManagement" });
        }

        // POST: Question Delete
        [HttpPost("QuestionDelete/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> QuestionDelete(int id)
        {
            var question = await _unitOfWork.Questions.GetByIdAsync(id);
            if (question == null)
            {
                TempData["ErrorMessage"] = "Question not found.";
                return RedirectToAction("Index", new { scrollTo = "QuestionsManagement" });
            }

            // Delete associated media files
            if (!string.IsNullOrEmpty(question.ImageUrl))
            {
                DeleteQuizMediaFile(question.ImageUrl);
            }
            if (!string.IsNullOrEmpty(question.AudioUrl))
            {
                DeleteQuizMediaFile(question.AudioUrl);
            }

            // Delete the question (answers will be cascade deleted if configured)
            _unitOfWork.Questions.Remove(question);
            TempData["SuccessMessage"] = "Question deleted successfully!";
            return RedirectToAction("Index", new { scrollTo = "QuestionsManagement" });
        }
        #endregion

        #region File Management
        // Helper method for image validation and saving
        private async Task<string> ValidateAndSaveImage(IFormFile image, string existingImagePath, string imageType)
        {
            if (image == null) return existingImagePath;  // No new image uploaded, return the existing one.

            // Validate image format and size
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
            var fileExtension = Path.GetExtension(image.FileName).ToLower();

            if (!allowedExtensions.Contains(fileExtension) || image.Length > 2 * 1024 * 1024)
            {
                ModelState.AddModelError(imageType, "Invalid image format or size.");
               
                return existingImagePath;
            }

            // Save new image
            string newImagePath = await SaveFile(image);

            // Delete old image if it exists
            if (!string.IsNullOrEmpty(existingImagePath))
            {
                var oldImagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "img", existingImagePath);
                if (System.IO.File.Exists(oldImagePath))
                {
                    System.IO.File.Delete(oldImagePath);
                    Console.WriteLine($"[INFO] Deleted old {imageType} image: {oldImagePath}");
                }
            }

            Console.WriteLine($"[INFO] Updated {imageType} image path: {newImagePath}");
            return newImagePath;
        }
        private void DeleteFile(string filePath, string fileType)
        {
            if (!string.IsNullOrEmpty(filePath))
            {
                var fullPath = Path.Combine(_env.WebRootPath, "img", filePath.TrimStart('/'));
                if (System.IO.File.Exists(fullPath))
                {
                    try
                    {
                        System.IO.File.Delete(fullPath);
                        Console.WriteLine($"[INFO] Deleted {fileType} file: {fullPath}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[ERROR] Failed to delete {fileType} file: {fullPath}. Exception: {ex.Message}");
                    }
                }
                else
                {
                    Console.WriteLine($"[WARN] {fileType} file not found at: {fullPath}");
                }
            }
            else
            {
                Console.WriteLine($"[INFO] No {fileType} file associated.");
            }
        }


        // Helper to save files
        private async Task<string> SaveFile(IFormFile file)
        {
            var uploadsFolder = Path.Combine(_env.WebRootPath, "img");
            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            var filePath = Path.Combine(uploadsFolder, fileName);

            Directory.CreateDirectory(uploadsFolder);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return $"{fileName}";
        }
        private void SaveAllTranslations(IManageResourceService resourceService, int baseKey, CarouselViewModel model)
        {
            // Validate baseKey
            if (baseKey <= 0)
            {
                Console.WriteLine("[ERROR] Invalid baseKey provided for translations.");
                return;
            }

            // Prepare translations
            var translations = new Dictionary<string, (string Title, string Description, string LinkText)>
            {
                { "tr-TR", (model.TranslationsTR.Title, model.TranslationsTR.Description, model.TranslationsTR.LinkText) },
                { "de-DE", (model.TranslationsDE.Title, model.TranslationsDE.Description, model.TranslationsDE.LinkText) }
            };

            foreach (var translation in translations)
            {
                var culture = translation.Key;
                var (title, description, linkText) = translation.Value;

                // Unique keys with carousel prefix
                string keyPrefix = $"Carousel_{baseKey}";

                if (!string.IsNullOrEmpty(title))
                    SaveTranslation(resourceService, $"{keyPrefix}_Title", title, culture);

                if (!string.IsNullOrEmpty(description))
                    SaveTranslation(resourceService, $"{keyPrefix}_Description", description, culture);

                if (!string.IsNullOrEmpty(linkText))
                    SaveTranslation(resourceService, $"{keyPrefix}_LinkText", linkText, culture);

                Console.WriteLine($"[INFO] Saved translations for culture '{culture}' with key prefix '{keyPrefix}'");
            }
        }
        private void SaveTranslation(IManageResourceService resourceService, string key, string value, string culture)
        {
            Console.WriteLine($"[DEBUG] Saving translation: Key='{key}', Value='{value}', Culture='{culture}'");

            if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(value) || string.IsNullOrEmpty(culture))
            {
                Console.WriteLine($"[ERROR] Invalid translation data. Key: '{key}', Value: '{value}', Culture: '{culture}'");
                return;
            }

            resourceService.AddOrUpdate(key, value, culture); // Save key-value using IManageResourceService
        }
    #endregion

    }
}