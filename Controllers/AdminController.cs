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
                return RedirectToAction("Account");
            }

            // Get the current logged-in user
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                TempData["ErrorMessage"] = "Current user not found.";
                return RedirectToAction("Account");
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
                return RedirectToAction("Account");
            }

            // Additional protection: Don't allow users to delete themselves
            if (user.Id == currentUser.Id)
            {
                TempData["ErrorMessage"] = "You cannot delete your own account.";
                return RedirectToAction("Account");
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

            return RedirectToAction("Account");
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
        private void DeleteUnusedImages(Blog blog, List<string> usedImagePaths)
        {
            // Paths for images
            string imgPath = Path.Combine(_env.WebRootPath, "blog", "img");
            string gifPath = Path.Combine(_env.WebRootPath, "blog", "gif");

            // Check for old images not used anymore
            string[] imgFiles = Directory.GetFiles(imgPath);
            string[] gifFiles = Directory.GetFiles(gifPath);

            // Merge files
            var allFiles = imgFiles.Concat(gifFiles).ToList();

            foreach (var file in allFiles)
            {
                string relativePath = file.Replace(_env.WebRootPath, "").Replace("\\", "/").TrimStart('/');
                if (!usedImagePaths.Contains(relativePath))
                {
                    System.IO.File.Delete(file);
                    Console.WriteLine($"[DEBUG] Deleted unused image: {file}");
                }
            }
        }


        private string ProcessContentImagesForEdit(string content)
        {
            string imgPath = "/blog/img/";
            string gifPath = "/blog/gif/";

            // Replace temporary paths with permanent paths
            content = System.Text.RegularExpressions.Regex.Replace(content, @"/temp/(.*?\.(jpg|jpeg|png))", $"{imgPath}$1");
            content = System.Text.RegularExpressions.Regex.Replace(content, @"/temp/(.*?\.(gif))", $"{gifPath}$1");

            Console.WriteLine("[DEBUG] Fixed image paths in content for edit.");
            return content;
        }


        // ProcessContentImages Method
        private string ProcessContentImages(string content, Dictionary<string, string> fileMappings)
        {
            if (string.IsNullOrEmpty(content))
            {
                return content; // Return unchanged if content is null or empty
            }

            string tempPath = Path.Combine(_env.WebRootPath, "temp");
            string imgPath = Path.Combine(_env.WebRootPath, "blog", "img");
            string gifPath = Path.Combine(_env.WebRootPath, "blog", "gif");

            // Ensure directories exist
            Directory.CreateDirectory(imgPath);
            Directory.CreateDirectory(gifPath);

            // Replace mapped URLs first
            foreach (var mapping in fileMappings)
            {
                // Replace temp URLs in the content with final URLs
                content = content.Replace(mapping.Key, mapping.Value);

                // Move the file from the temp folder to the final destination
                string tempFilePath = Path.Combine(_env.WebRootPath, mapping.Key.TrimStart('/'));
                string finalFilePath = Path.Combine(_env.WebRootPath, mapping.Value.TrimStart('/'));
                // Ensure the directory exists before moving the file
                string? directoryPath = Path.GetDirectoryName(finalFilePath);
                if (directoryPath != null) // Check if it's not null
                {
                    Directory.CreateDirectory(directoryPath); // Create the directory
                }
                if (System.IO.File.Exists(tempFilePath) && !System.IO.File.Exists(finalFilePath))
                {
                    System.IO.File.Move(tempFilePath, finalFilePath); // Move the file
                    Console.WriteLine($"[DEBUG] Moved file from {tempFilePath} to {finalFilePath}");
                }
                else
                {
                    Console.WriteLine($"[WARNING] File move failed: {tempFilePath} to {finalFilePath}");
                }
            }

            // Find all images in content
            var matches = Regex.Matches(content, @"src=[""'](?<url>/temp/.*?\.(jpg|jpeg|png|gif))[""']");
            foreach (Match match in matches)
            {
                string tempUrl = match.Groups["url"].Value; // Example: /temp/image.png
                string tempFilePath = Path.Combine(_env.WebRootPath, tempUrl.TrimStart('/'));

                if (System.IO.File.Exists(tempFilePath))
                {
                    string fileName = Path.GetFileName(tempFilePath);
                    string fileExtension = Path.GetExtension(fileName).ToLower();

                    // Determine destination path
                    string targetPath = fileExtension == ".gif"
                        ? Path.Combine(gifPath, fileName)
                        : Path.Combine(imgPath, fileName);

                    // Move file if it doesn't already exist
                    if (!System.IO.File.Exists(targetPath))
                    {
                        System.IO.File.Move(tempFilePath, targetPath); // Move file to final location
                        Console.WriteLine($"[DEBUG] Moved image: {tempFilePath} -> {targetPath}");
                    }

                    // Update content with the final URL
                    string finalUrl = $"/blog/{(fileExtension == ".gif" ? "gif" : "img")}/{fileName}";
                    content = content.Replace(tempUrl, finalUrl);
                }
                else
                {
                    Console.WriteLine($"[WARNING] Temp image not found: {tempFilePath}");
                }
            }

            return content; // Return updated content
        }



        [HttpPost("UploadFile")]
        public async Task<IActionResult> UploadFile(IFormFile upload, string blogId)
        {
            if (upload == null || upload.Length == 0)
            {
                return Json(new { uploaded = false, error = "No file uploaded." });
            }

            // Define Temp Path
            string tempPath = Path.Combine(_env.WebRootPath, "temp");
            Directory.CreateDirectory(tempPath); // Ensure temp folder exists

            // Calculate File Hash
            string fileHash;
            using (var md5 = System.Security.Cryptography.MD5.Create())
            {
                using (var stream = upload.OpenReadStream())
                {
                    fileHash = BitConverter.ToString(md5.ComputeHash(stream)).Replace("-", "").ToLower();
                }
            }

            // Generate Unique File Name Using Hash
            string extension = Path.GetExtension(upload.FileName);
            string fileName = $"{fileHash}{extension}";
            string tempFilePath = Path.Combine(tempPath, fileName);

            // Check if File Already Exists in Temp
            if (!System.IO.File.Exists(tempFilePath))
            {
                using (var fileStream = new FileStream(tempFilePath, FileMode.Create))
                {
                    await upload.CopyToAsync(fileStream);
                }
                Console.WriteLine($"[DEBUG] Uploaded new file: {fileName}");
            }
            else
            {
                Console.WriteLine($"[DEBUG] File already exists in temp: {fileName}");
            }

            // Return Temporary URL for CKEditor
            string tempUrl = $"/temp/{fileName}";
            return Json(new { uploaded = true, url = tempUrl });
        }

        private string MoveImagesAndFixPaths(string content, int blogId)
        {
            string tempPath = Path.Combine(_env.WebRootPath, "temp");
            string imgPath = Path.Combine(_env.WebRootPath, "blog", "img");
            string gifPath = Path.Combine(_env.WebRootPath, "blog", "gif");

            // Ensure directories exist
            Directory.CreateDirectory(imgPath);
            Directory.CreateDirectory(gifPath);

            // Find all image src attributes in the content
            var matches = System.Text.RegularExpressions.Regex.Matches(content, @"src=[""'](?<url>/temp/.*?\.(jpg|jpeg|png|gif))[""']");
            foreach (System.Text.RegularExpressions.Match match in matches)
            {
                string tempUrl = match.Groups["url"].Value; // Example: /temp/image123.gif
                string tempFilePath = Path.Combine(_env.WebRootPath, tempUrl.TrimStart('/'));

                // Move file if it exists
                if (System.IO.File.Exists(tempFilePath))
                {
                    string fileName = Path.GetFileName(tempFilePath);
                    string fileExtension = Path.GetExtension(fileName).ToLower();

                    // Determine destination folder based on file extension
                    string targetPath = fileExtension == ".gif"
                        ? Path.Combine(gifPath, fileName)
                        : Path.Combine(imgPath, fileName);

                    if (!System.IO.File.Exists(targetPath))
                    {
                        System.IO.File.Move(tempFilePath, targetPath);
                        Console.WriteLine($"[DEBUG] Moved image: {tempFilePath} -> {targetPath}");
                    }

                    // Replace temp URL with the final URL in the content
                    string finalUrl = $"/blog/{(fileExtension == ".gif" ? "gif" : "img")}/{fileName}";
                    content = content.Replace(tempUrl, finalUrl);
                }
                else
                {
                    Console.WriteLine($"[WARNING] Temp image not found: {tempFilePath}");
                }
            }

            return content; // Return processed content with updated paths
        }



        private void SaveContentToResx(BlogCreateModel model, int id)
        {
            // Supported languages and their codes
            string[] cultures = { "tr-TR", "de-DE" };
            string[] titles = { model.TitleTR, model.TitleDE };
            string[] contents = { model.ContentTR, model.ContentDE };

            // Process each translation
            for (int i = 0; i < cultures.Length; i++)
            {
                string culture = cultures[i];  // Language code
                string title = titles[i];      // Language-specific title
                string content = contents[i]; // Already processed content

                // Save translations to .resx
                _manageResourceService.AddOrUpdateResource($"Title_{id}_{model.Url}_{culture.Substring(0, 2).ToLower()}", title, culture);
                _manageResourceService.AddOrUpdateResource($"Content_{id}_{model.Url}_{culture.Substring(0, 2).ToLower()}", content, culture);

                Console.WriteLine($"[DEBUG] Translation saved for {culture} with updated image paths.");
            }

            Console.WriteLine("[DEBUG] All translations updated successfully.");
        }


        private void SaveContentToResxEdit(BlogResultModel model, string updatedContent, int id)
        {
            // Supported languages and their codes
            string[] cultures = { "tr-TR", "de-DE" };
            string[] langCodes = { "tr", "de" };
            string[] titles = { model.TitleTR, model.TitleDE };
            string[] contents = { model.ContentTR, model.ContentDE };

            for (int i = 0; i < cultures.Length; i++)
            {
                string culture = cultures[i];
                string langCode = langCodes[i]; // Language code for key
                string title = titles[i];
                string content = contents[i];

                // Process images in translations
                string processedContent = ProcessContentImagesForEdit(content);

                // Save translations to .resx
                _manageResourceService.AddOrUpdateResource($"Title_{id}_{model.Url}_{langCode}", title, culture);
                _manageResourceService.AddOrUpdateResource($"Content_{id}_{model.Url}_{langCode}", processedContent, culture);

                Console.WriteLine($"[DEBUG] Translation saved for {culture} with updated image paths.");
            }

            Console.WriteLine("[DEBUG] All translations updated successfully.");
        }

        private List<string> ExtractImagesFromTranslations(int blogId, string url)
        {
            List<string> imagePaths = new();
            var cultures = new Dictionary<string, string>
            {
                { "tr-TR", "tr" },
                { "de-DE", "de" }
            };

            foreach (var culture in cultures)
            {
                string contentKey = $"Content_{blogId}_{url}_{culture.Value}";
                string translationContent = _manageResourceService.ReadResourceValue(contentKey, culture.Key);

                if (!string.IsNullOrEmpty(translationContent))
                {
                    var matches = System.Text.RegularExpressions.Regex.Matches(
                        translationContent,
                        @"src=[""'](?<url>/blog/(img|gif)/.*?\.(jpg|jpeg|png|gif))[""']");
                    foreach (System.Text.RegularExpressions.Match match in matches)
                    {
                        imagePaths.Add(match.Groups["url"].Value.TrimStart('/'));
                    }
                }
            }

            return imagePaths; // Return all extracted image paths
        }


        private async Task DeleteOrphanedImages()
        {
            string imgFolder = Path.Combine(_env.WebRootPath, "blog", "img");
            string gifFolder = Path.Combine(_env.WebRootPath, "blog", "gif");

            // Collect all image paths in the folders
            var allImages = Directory.GetFiles(imgFolder, "*.*", SearchOption.TopDirectoryOnly)
                                    .Union(Directory.GetFiles(gifFolder, "*.*", SearchOption.TopDirectoryOnly))
                                    .ToList();

            // Extract used images from translations
            var usedImages = new HashSet<string>();
            var blogs = await _unitOfWork.Blogs.GetAllAsync(); // Get all blogs
            foreach (var blog in blogs)
            {
                usedImages.UnionWith(ExtractImagesFromTranslations(blog.BlogId, blog.Url));
            }

            // Compare and delete unused images
            foreach (var image in allImages)
            {
                string relativePath = image.Replace(_env.WebRootPath, "").Replace("\\", "/").TrimStart('/');

                if (!usedImages.Contains(relativePath))
                {
                    try
                    {
                        System.IO.File.Delete(image);
                        Console.WriteLine($"[DEBUG] Deleted orphaned image: {image}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[ERROR] Failed to delete orphaned image {image}: {ex.Message}");
                    }
                }
            }
        }


        private void DeleteImages(Blog blog)
        {
            // Paths for image storage
            string imgPath = Path.Combine(_env.WebRootPath, "blog", "img");
            string gifPath = Path.Combine(_env.WebRootPath, "blog", "gif");
            string coverPath = Path.Combine(_env.WebRootPath, "img");

            // 1. Delete Cover Image
            if (!string.IsNullOrEmpty(blog.Image))
            {
                string coverImagePath = Path.Combine(coverPath, blog.Image);
                if (System.IO.File.Exists(coverImagePath))
                {
                    System.IO.File.Delete(coverImagePath);
                    Console.WriteLine($"[DEBUG] Deleted cover image: {coverImagePath}");
                }
                else
                {
                    Console.WriteLine($"[WARNING] Cover image not found: {coverImagePath}");
                }
            }

            // 2. Delete Embedded Images in Content and Translations
            // Supported cultures and keys
            var cultures = new Dictionary<string, string>
            {
                { "tr-TR", "tr" },
                { "de-DE", "de" }
            };

            // Track deleted images to avoid multiple deletions
            HashSet<string> deletedImages = new HashSet<string>();

            // Iterate through each translation and process its images
            foreach (var culture in cultures)
            {
                // Generate translation content key
                string contentKey = $"Content_{blog.BlogId}_{blog.Url}_{culture.Value}";

                // Read the content from .resx
                string translationContent = _manageResourceService.ReadResourceValue(contentKey, culture.Key);

                if (string.IsNullOrEmpty(translationContent))
                {
                    Console.WriteLine($"[WARNING] No content found for key {contentKey} in culture {culture.Key}");
                    continue;
                }

                // Extract all image URLs using Regex
                var matches = System.Text.RegularExpressions.Regex.Matches(
                    translationContent,
                    @"src=[""'](?<url>/blog/(img|gif)/.*?\.(jpg|jpeg|png|gif))[""']");

                // Process each image
                foreach (System.Text.RegularExpressions.Match match in matches)
                {
                    string imageUrl = match.Groups["url"].Value; // e.g., /blog/img/example1.jpg
                    string imagePath = Path.Combine(_env.WebRootPath, imageUrl.TrimStart('/')); // Get full path

                    // Skip if already deleted
                    if (deletedImages.Contains(imagePath))
                    {
                        continue; // Avoid duplicate deletions
                    }

                    if (System.IO.File.Exists(imagePath))
                    {
                        try
                        {
                            System.IO.File.Delete(imagePath); // Delete file
                            deletedImages.Add(imagePath);    // Track deleted image
                            Console.WriteLine($"[DEBUG] Deleted embedded image: {imagePath}");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[ERROR] Failed to delete image {imagePath}: {ex.Message}");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"[WARNING] Embedded image not found: {imagePath}");
                    }
                }
            }

            Console.WriteLine("[DEBUG] All images related to the blog and translations deleted.");
        }



        private List<string> ExtractImagePathsFromContent(string content)
        {
            var imagePaths = new List<string>();

            if (!string.IsNullOrEmpty(content))
            {
                var matches = System.Text.RegularExpressions.Regex.Matches(content, @"src=[""'](?<url>/blog/(img|gif)/.*?\.(jpg|jpeg|png|gif))[""']");
                foreach (System.Text.RegularExpressions.Match match in matches)
                {
                    imagePaths.Add(match.Groups["url"].Value.TrimStart('/')); // Add relative path
                }
            }

            return imagePaths;
        }


        private void DeleteTranslations(int id, string url)
        {
            // Supported languages and their culture codes
            var cultures = new Dictionary<string, string>
            {
                { "tr-TR", "tr" },
                { "de-DE", "de" }
            };

            // Loop through each culture and delete its translations
            foreach (var culture in cultures)
            {
                string cultureCode = culture.Value; // 'en', 'tr', etc.
                string lang = culture.Key;         // 'en-US', 'tr-TR', etc.

                // Construct keys with culture suffix
                string titleKey = $"Title_{id}_{url}_{cultureCode}";
                string contentKey = $"Content_{id}_{url}_{cultureCode}";

                // Delete resources for the current language
                _manageResourceService.DeleteResource(titleKey, lang);
                _manageResourceService.DeleteResource(contentKey, lang);

                Console.WriteLine($"[DEBUG] Deleted translations for Blog ID {id} in culture {lang}");
            }
        }

        private void DeleteUnusedImages(List<string> unusedImages)
        {
            foreach (var imgPath in unusedImages)
            {
                string fullPath = Path.Combine(_env.WebRootPath, imgPath.TrimStart('/'));
                if (System.IO.File.Exists(fullPath))
                {
                    System.IO.File.Delete(fullPath);
                    Console.WriteLine($"[DEBUG] Deleted unused image: {fullPath}");
                }
                else
                {
                    Console.WriteLine($"[WARNING] File not found: {fullPath}");
                }
            }
        }

        [HttpGet("GetQuizQuestions")]
        public async Task<IActionResult> GetQuizQuestions(int quizId)
        {
            // Use a repository method that loads the quiz including its questions.
            var quiz = await _unitOfWork.Quizzes.GetByIdWithQuestions(quizId);
            if (quiz == null)
            {
                return NotFound();
            }

            var questions = quiz.Questions.Select(q => new
            {
                QuestionId = q.Id,
                QuestionText = q.QuestionText
            }).ToList();

            return Json(questions);
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
            var currentUser = await _userManager.GetUserAsync(User);
            var users = _userManager.Users.ToList();
            model.TeacherOptions = users.Select(u => new SelectListItem
            {
                Value = u.Id,
                Text = (!string.IsNullOrWhiteSpace(u.FirstName) || !string.IsNullOrWhiteSpace(u.LastName)) ?
                        string.Join(' ', new[] { u.FirstName, u.LastName }.Where(s => !string.IsNullOrWhiteSpace(s))) :
                        (u.UserName ?? u.Email ?? "(unknown)")
            }).ToList();
            if (currentUser != null)
            {
                model.TeacherId = currentUser.Id;
                model.TeacherName = string.Join(' ', new[] { currentUser.FirstName, currentUser.LastName }.Where(s => !string.IsNullOrWhiteSpace(s)));
                if (string.IsNullOrWhiteSpace(model.TeacherName)) model.TeacherName = currentUser.UserName ?? currentUser.Email ?? string.Empty;
            }

            return View(model);
        }

        [HttpPost("QuizCreate")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> QuizCreate(QuizCreateViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            // Determine the final TeacherId to use
            string? finalTeacherId = null;
            
            if (model.IsAnonymous)
            {
                // Anonymous quiz - no teacher
                finalTeacherId = null;
            }
            else if (!string.IsNullOrWhiteSpace(model.TeacherId))
            {
                // Validate that the selected teacher exists
                var selectedTeacher = await _userManager.FindByIdAsync(model.TeacherId);
                if (selectedTeacher != null)
                {
                    finalTeacherId = model.TeacherId;
                }
                else
                {
                    // Teacher not found, use current user as fallback
                    _logger.LogWarning("Selected TeacherId '{TeacherId}' not found. Using current user '{UserId}'.", model.TeacherId, user.Id);
                    finalTeacherId = user.Id;
                }
            }
            else
            {
                // No teacher selected, default to current user
                finalTeacherId = user.Id;
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
                    var quiz = new SpeakingClub.Entity.Quiz
                    {
                        Title = model.Title,
                        Description = model.Description,
                        TeacherId = finalTeacherId,
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
                // Teacher fields
                TeacherId = quiz.TeacherId,
                IsAnonymous = string.IsNullOrWhiteSpace(quiz.TeacherId),

                Categories = (await _unitOfWork.Categories.GetAllAsync())
                    .Select(c => new SelectListItem(c.Name, c.CategoryId.ToString())),
                Tags = (await _unitOfWork.Tags.GetAllAsync())
                    .Select(t => new SelectListItem(t.Name, t.TagId.ToString())),
                Words = (await _unitOfWork.Words.GetAllAsync())
                    .Select(w => new SelectListItem(w.Term, w.WordId.ToString()))
            };

            // Populate teacher selection options (show user display names)
            var users = _userManager.Users.ToList();
            model.TeacherOptions = users.Select(u => new SelectListItem
            {
                Value = u.Id,
                Text = (!string.IsNullOrWhiteSpace(u.FirstName) || !string.IsNullOrWhiteSpace(u.LastName)) ?
                        string.Join(' ', new[] { u.FirstName, u.LastName }.Where(s => !string.IsNullOrWhiteSpace(s))) :
                        (u.UserName ?? u.Email ?? "(unknown)")
            }).ToList();

            // Resolve teacher name if teacher is set
            if (!string.IsNullOrWhiteSpace(model.TeacherId))
            {
                var teacher = await _userManager.FindByIdAsync(model.TeacherId);
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

                // Handle teacher assignment
                var originalTeacherId = quiz.TeacherId;
                bool teacherChanged = false;
                if (model.IsAnonymous)
                {
                    if (!string.IsNullOrWhiteSpace(originalTeacherId))
                    {
                        quiz.TeacherId = null;
                        teacherChanged = true;
                    }
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(model.TeacherId) && model.TeacherId != originalTeacherId)
                    {
                        quiz.TeacherId = model.TeacherId;
                        teacherChanged = true;
                    }
                }

                _unitOfWork.Quizzes.Update(quiz, modifyTeacherId: teacherChanged);
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
        [HttpGet("Admin/CarouselCreate")]
        public IActionResult CarouselCreate()
        {
            return View(new CarouselViewModel());
        }


        [HttpPost("Admin/CarouselCreate")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CarouselCreate(CarouselViewModel model, [FromServices] IManageResourceService resourceService)
        {
            if (!ModelState.IsValid)
            {
                Console.WriteLine("[WARN] Model validation failed.");
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
                // Handle image uploads
                carousel.CarouselImage = model.CarouselImage != null ? await SaveFile(model.CarouselImage) : string.Empty;
                carousel.CarouselImage600w = model.CarouselImage600w != null ? await SaveFile(model.CarouselImage600w) : string.Empty;
                carousel.CarouselImage1200w = model.CarouselImage1200w != null ? await SaveFile(model.CarouselImage1200w) : string.Empty;

                // Save to database and retrieve entity with assigned ID
                carousel = await _unitOfWork.Slides.CreateAndReturn(carousel);

                // Ensure the ID is valid
                int carouselId = carousel.SlideId; // Correct ID from DB
                if (carouselId <= 0)
                {
                    Console.WriteLine("[ERROR] Failed to generate a valid ID for carousel.");
                    TempData["ErrorMessage"] = "An error occurred while creating the carousel.";
                    return View(model);
                }

                Console.WriteLine($"[INFO] Carousel created successfully with ID: {carouselId}");

                // Save translations using the valid ID
                SaveAllTranslations(_manageResourceService, carouselId, model);

                TempData["SuccessMessage"] = "Carousel created successfully!";
                return RedirectToAction("Carousels");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to create carousel. Exception: {ex.Message}");
                TempData["ErrorMessage"] = "An error occurred while creating the carousel.";
                return View(model);
            }
        }



        [HttpGet("Admin/CarouselEdit/{id}")]
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




        [HttpPost("Admin/CarouselEdit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CarouselEdit(CarouselEditModel model)
        {
            Console.WriteLine($"[INFO] Processing update for Carousel ID: {model.CarouselId}");

            if (!ModelState.IsValid)
            {
                Console.WriteLine("[WARN] Model validation failed.");
                return View(model);
            }

            var carousel = await _unitOfWork.Slides.GetByIdAsync(model.CarouselId);
            if (carousel == null)
            {
                Console.WriteLine($"[WARN] Carousel with ID {model.CarouselId} not found.");
                return NotFound();
            }

            // Update carousel properties
            carousel.CarouselTitle = model.CarouselTitle;
            carousel.CarouselDescription = model.CarouselDescription;
            carousel.CarouselLink = model.CarouselLink;
            carousel.CarouselLinkText = model.CarouselLinkText;

            // Image validation and update
            carousel.CarouselImage = await ValidateAndSaveImage(model.CarouselImage, carousel.CarouselImage, "CarouselImage");
            carousel.CarouselImage1200w = await ValidateAndSaveImage(model.CarouselImage1200w, carousel.CarouselImage1200w, "CarouselImage1200w");
            carousel.CarouselImage600w = await ValidateAndSaveImage(model.CarouselImage600w, carousel.CarouselImage600w, "CarouselImage600w");

            // Update translations
            UpdateTranslations(carousel.SlideId, model);

            // Save carousel
            await _unitOfWork.Slides.UpdateAsync(carousel);
            Console.WriteLine($"[INFO] Carousel with ID {model.CarouselId} updated successfully!");
            TempData["SuccessMessage"] = "Carousel updated successfully!";
            return RedirectToAction("Carousels");
        }


        [HttpPost("CarouselDelete/{id}")]
        public async Task<IActionResult> CarouselDelete(int id)
        {
            Console.WriteLine($"[INFO] Starting deletion process for Carousel ID: {id}");

            // Step 1: Retrieve the carousel
            var carousel = await _unitOfWork.Slides.GetByIdAsync(id);
            if (carousel == null)
            {
                Console.WriteLine($"[WARN] Carousel with ID {id} not found.");
                TempData["ErrorMessage"] = "Carousel not found.";
                return RedirectToAction("Carousels");
            }

            Console.WriteLine($"[INFO] Found Carousel - Title: {carousel.CarouselTitle}");

            try
            {
                // Step 2: Delete associated files
                DeleteFile(carousel.CarouselImage, "CarouselImage");
                DeleteFile(carousel.CarouselImage600w, "CarouselImage600w");
                DeleteFile(carousel.CarouselImage1200w, "CarouselImage1200w");

                // Step 3: Delete translations using IManageResourceService
                var baseKey = $"Carousel_{carousel.SlideId}";

                // Languages
                string[] cultures = { "en-US", "tr-TR", "de-DE", "fr-FR", "ar-SA" };
                string[] keys = { "Title", "Description", "LinkText" };

                foreach (var culture in cultures)
                {
                    foreach (var key in keys)
                    {
                        string resourceKey = $"{baseKey}_{key}";
                        var success = _manageResourceService.Delete(resourceKey, culture); // Using IManageResourceService for deletion
                        if (success)
                        {
                            Console.WriteLine($"[INFO] Successfully deleted translation: {resourceKey} in {culture}");
                        }
                        else
                        {
                            Console.WriteLine($"[WARN] Failed to delete translation: {resourceKey} in {culture}");
                        }
                    }
                }

                // Step 4: Delete the carousel from the database
                await _unitOfWork.Slides.DeleteAsync(id);
                Console.WriteLine($"[INFO] Carousel with ID {id} deleted successfully!");

                TempData["SuccessMessage"] = "Carousel deleted successfully!";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to delete carousel with ID {id}. Exception: {ex.Message}");
                TempData["ErrorMessage"] = "An error occurred while deleting the carousel.";
            }

            // Step 5: Redirect to the carousels page
            Console.WriteLine("[INFO] Redirecting to Carousels page...");
            return RedirectToAction("Carousels");
        }

        #endregion
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
        private void UpdateTranslations(int id, CarouselEditModel model)
        {
            var baseKey = $"Carousel_{id}";
            var translations = new Dictionary<string, (string Title, string Desc, string LinkText)> {
                { "tr-TR", (model.CarouselTitleTR, model.CarouselDescriptionTR, model.CarouselLinkTextTR) },
                { "de-DE", (model.CarouselTitleDE, model.CarouselDescriptionDE, model.CarouselLinkTextDE) }
            };

            foreach (var kvp in translations)
            {
                var culture = kvp.Key;
                var (title, desc, linkText) = kvp.Value;

                if (!string.IsNullOrWhiteSpace(title))
                    _manageResourceService.AddOrUpdate($"{baseKey}_Title", title, culture);
                if (!string.IsNullOrWhiteSpace(desc))
                    _manageResourceService.AddOrUpdate($"{baseKey}_Description", desc, culture);
                if (!string.IsNullOrWhiteSpace(linkText))
                    _manageResourceService.AddOrUpdate($"{baseKey}_LinkText", linkText, culture);
            }
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