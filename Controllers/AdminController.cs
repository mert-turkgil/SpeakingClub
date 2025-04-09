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

        public AdminController(ILogger<AdminController> logger,UserManager<User> userManager, RoleManager<IdentityRole> roleManager, IUnitOfWork unitOfWork,IWebHostEnvironment env,
        IManageResourceService manageResourceService,AliveResourceService dynamicResourceService)
        {
            _env = env;
            _dynamicResourceService =dynamicResourceService;
            _manageResourceService=manageResourceService;
            _roleManager= roleManager;
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



        private void SaveContentToResx(BlogCreateModel model , int id)
        {
            // Supported languages and their codes
            string[] cultures = { "en-US", "tr-TR", "de-DE"};
            string[] titles = { model.TitleUS, model.TitleTR, model.TitleDE };
            string[] contents = { model.ContentUS, model.ContentTR, model.ContentDE };

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
            string[] cultures = { "en-US", "tr-TR", "de-DE", "fr-FR" };
            string[] langCodes = { "en", "tr", "de", "fr" };
            string[] titles = { model.TitleUS, model.TitleTR, model.TitleDE};
            string[] contents = { model.ContentUS, model.ContentTR, model.ContentDE};

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
                { "en-US", "en" },
                { "tr-TR", "tr" },
                { "de-DE", "de" },
                { "fr-FR", "fr" }
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
                { "en-US", "en" },
                { "tr-TR", "tr" },
                { "de-DE", "de" },
                { "fr-FR", "fr" }
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
                { "en-US", "en" },
                { "tr-TR", "tr" },
                { "de-DE", "de" },
                { "fr-FR", "fr" }
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
                    var  selectedQuizzes = Quizzes
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
        [HttpGet("QuizCreate")]
        public async Task<IActionResult> QuizCreate()
        {
            var model = new QuizCreateViewModel
            {
                Title = string.Empty,
                Description = string.Empty,
                Categories = (await _unitOfWork.Categories.GetAllAsync())
                    .Select(c => new SelectListItem(c.Name, c.CategoryId.ToString())),
                Tags = (await _unitOfWork.Tags.GetAllAsync())
                    .Select(t => new SelectListItem(t.Name, t.TagId.ToString())),
                Words = (await _unitOfWork.Words.GetAllAsync())
                    .Select(w => new SelectListItem(w.Term, w.WordId.ToString()))
            };
            model.Questions.Add(new QuestionViewModel
            {
                QuestionText = string.Empty, 
                Answers = new List<AnswerViewModel>
                {
                    new AnswerViewModel { AnswerText = string.Empty },
                    new AnswerViewModel { AnswerText = string.Empty }
                }
            });

            return View(model);
        }

        [HttpPost("QuizCreate")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> QuizCreate(QuizCreateViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            if (ModelState.IsValid)
            {
                using var transaction = await _unitOfWork.BeginTransactionAsync();
                try
                {
                    var audioUrl = model.AudioFile != null ? await ProcessAudioUpload(model.AudioFile) : null;
                    var taglist = await _unitOfWork.Tags.GetAllAsync();
                    var wordlist = await _unitOfWork.Words.GetAllAsync();
                    var quiz = new SpeakingClub.Entity.Quiz
                    {
                        Title = model.Title,
                        Description = model.Description,
                        TeacherId = user.Id,
                        CategoryId = model.CategoryId,
                        Tags = taglist.Where(t => model.SelectedTagIds.Contains(t.TagId)).ToList(),
                        Words = wordlist.Where(w => model.SelectedWordIds.Contains(w.WordId)).ToList()
                    };

                        foreach (var questionModel in model.Questions)
                        {
                            var question = new SpeakingClub.Entity.Question // Use Entity namespace
                            {
                                QuestionText = questionModel.QuestionText,
                                ImageUrl = questionModel.ImageUrl,
                                AudioUrl = questionModel.AudioUrl,
                                VideoUrl = questionModel.VideoUrl,
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
                    return RedirectToAction("QuizList");
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    ModelState.AddModelError("", $"Error creating quiz: {ex.Message}");
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

        #region Mp3
        private async Task<string?> ProcessAudioUpload(IFormFile audioFile)
        {
            if (audioFile == null || audioFile.Length == 0)
                return null;

            var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "audio");
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            var uniqueFileName = $"{Guid.NewGuid()}{Path.GetExtension(audioFile.FileName)}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await audioFile.CopyToAsync(fileStream);
            }

            return $"/uploads/audio/{uniqueFileName}";
        }
        #endregion

        [HttpGet("QuizEdit/{id}")]
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
                        IsCorrect = a.IsCorrect
                    }).ToList()
                }).ToList(),
                Categories = (await _unitOfWork.Categories.GetAllAsync())
                    .Select(c => new SelectListItem(c.Name, c.CategoryId.ToString())),
                Tags = (await _unitOfWork.Tags.GetAllAsync())
                    .Select(t => new SelectListItem(t.Name, t.TagId.ToString())),
                Words = (await _unitOfWork.Words.GetAllAsync())
                    .Select(w => new SelectListItem(w.Term, w.WordId.ToString()))
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> QuizEdit(QuizEditViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

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
                quiz.TeacherId = user.Id;

                // Update relationships
                await UpdateQuizRelationships(quiz, model);

                // Update questions and answers
                await UpdateQuestionsAndAnswers(quiz, model);

                _unitOfWork.Quizzes.Update(quiz);
                await _unitOfWork.SaveAsync();
                await transaction.CommitAsync();

                TempData["SuccessMessage"] = "Quiz updated successfully!";
                return RedirectToAction("QuizList");
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

        private Task UpdateQuestionsAndAnswers(SpeakingClub.Entity.Quiz quiz, QuizEditViewModel model)
        {
            // Remove deleted questions
            var questionIds = model.Questions.Select(q => q.QuestionId).ToList();
            var questionsToRemove = quiz.Questions.Where(q => !questionIds.Contains(q.Id)).ToList();
            foreach (var question in questionsToRemove)
            {
                quiz.Questions.Remove(question);
            }

            foreach (var questionModel in model.Questions)
            {
                var question = quiz.Questions.FirstOrDefault(q => q.Id == questionModel.QuestionId) ?? new Entity.Question();
                
                question.QuestionText = questionModel.QuestionText;
                question.ImageUrl = questionModel.ImageUrl;
                question.AudioUrl = questionModel.AudioUrl;
                question.VideoUrl = questionModel.VideoUrl;

                // Update answers
                var answerIds = questionModel.Answers.Select(a => a.AnswerId).ToList();
                var answersToRemove = question.Answers.Where(a => answerIds.Contains(a.Id)).ToList();
                foreach (var answer in answersToRemove)
                {
                    question.Answers.Remove(answer);
                }

                foreach (var answerModel in questionModel.Answers)
                {
                    var answer = question.Answers.FirstOrDefault(a => a.Id == answerModel.AnswerId) ?? new Entity.QuizAnswer();
                    answer.AnswerText = answerModel.AnswerText;
                    answer.IsCorrect = answerModel.IsCorrect;
                    
                    if (answer.Id == 0)
                    {
                        question.Answers.Add(answer);
                    }
                }

                if (question.Id == 0)
                {
                    quiz.Questions.Add(question);
                }
            }

            return Task.CompletedTask;
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
  
        #region Blog
            #region Blog Create
[HttpGet("BlogCreate")]
public async Task<IActionResult> BlogCreate()
{
    // Fetch Categories, Quizzes, and Tags for selection lists.
    var categories = await _unitOfWork.Categories.GetAllAsync();

    // Ensure quizzes are eagerly loaded with questions.
    var quizzes = await _unitOfWork.Quizzes.GetAllAsync(); 
    var tags = await _unitOfWork.Tags.GetAllAsync();

    // Debug log: count total questions loaded.
    int totalQuestions = quizzes.Sum(q => q.Questions?.Count ?? 0);
    Console.WriteLine($"[DEBUG] Total quiz questions loaded: {totalQuestions}");

    // Populate ViewBag for Categories.
    ViewBag.Categories = categories.Select(c => new SelectListItem
    {
        Value = c.CategoryId.ToString(),
        Text = c.Name
    }).ToList();

    // Populate ViewBag for Quizzes (for single selection).
    ViewBag.Quizzes = quizzes.Select(q => new SelectListItem
    {
        Value = q.Id.ToString(),
        Text = q.Title
    }).ToList();

    // Aggregate quiz questions from all quizzes.
    var quizQuestions = quizzes
        .Where(q => q.Questions != null && q.Questions.Any())
        .SelectMany(q => q.Questions.Select(quest => new
        {
            QuestionId = quest.Id,
            QuestionText = quest.QuestionText,
            QuizId = q.Id
        }))
        .ToList();

    Console.WriteLine($"[DEBUG] Aggregated quiz questions count: {quizQuestions.Count}");
    foreach (var item in quizQuestions)
    {
        Console.WriteLine($"[DEBUG] Question - Id: {item.QuestionId}, Text: {item.QuestionText}, QuizId: {item.QuizId}");
    }
    ViewBag.QuizQuestions = quizQuestions;

    // Populate ViewBag for Tags.
    ViewBag.Tags = tags.Select(t => new SelectListItem
    {
        Value = t.TagId.ToString(),
        Text = t.Name
    }).ToList();

    // Create an initial empty BlogCreateModel.
    var model = new BlogCreateModel
    {
        SelectedCategoryIds = new List<int>(),
        // Change: Instead of multiple quiz IDs, allow single quiz selection.
        // You may add a new property, e.g. SelectedQuizId (int?) instead of SelectedQuizIds (List<int>).
        // For this example, we'll assume you add:
        // public int? SelectedQuizId { get; set; }
        // And then remove the checkbox UI for quizzes.
        SelectedQuizIds = null,
        SelectedTagIds = new List<int>(),
        IsHome = false,         // Default: not shown on home page.
        SelectedQuestionId = null // No quiz question selected by default.
    };

    Console.WriteLine("[DEBUG] BlogCreate GET page loaded successfully.");
    return View(model);
}

            [HttpPost("BlogCreate")]
            public async Task<IActionResult> BlogCreate(BlogCreateModel model)
            {
                if (!ModelState.IsValid)
                {
                    Console.WriteLine("[ERROR] BlogCreate model validation failed.");

                    // Fetch categories, quizzes, tags again for dropdowns.
                    var categories = await _unitOfWork.Categories.GetAllAsync();
                    var quizzes = await _unitOfWork.Quizzes.GetAllAsync();
                    var tags = await _unitOfWork.Tags.GetAllAsync();

                    ViewBag.Categories = categories.Select(c => new SelectListItem
                    {
                        Value = c.CategoryId.ToString(),
                        Text = c.Name
                    }).ToList();

                    ViewBag.Quizzes = quizzes.Select(q => new SelectListItem
                    {
                        Value = q.Id.ToString(),
                        Text = q.Title
                    }).ToList();

                    ViewBag.Tags = tags.Select(t => new SelectListItem
                    {
                        Value = t.TagId.ToString(),
                        Text = t.Name
                    }).ToList();

                    return View(model);
                }

                // Define necessary paths.
                string tempPath = Path.Combine(_env.WebRootPath, "temp");
                string imgPath = Path.Combine(_env.WebRootPath, "blog", "img");
                string gifPath = Path.Combine(_env.WebRootPath, "blog", "gif");
                string coverPath = Path.Combine(_env.WebRootPath, "img");

                Directory.CreateDirectory(tempPath);
                Directory.CreateDirectory(imgPath);
                Directory.CreateDirectory(gifPath);
                Directory.CreateDirectory(coverPath);

                var fileMappings = new Dictionary<string, string>();
                foreach (var file in Directory.GetFiles(tempPath))
                {
                    var fileName = Path.GetFileName(file);
                    var fileExtension = Path.GetExtension(file).ToLower();

                    var destination = fileExtension == ".gif"
                        ? Path.Combine(gifPath, fileName)
                        : Path.Combine(imgPath, fileName);

                    if (!System.IO.File.Exists(destination))
                    {
                        System.IO.File.Move(file, destination);
                        Console.WriteLine($"[DEBUG] Moved content image: {fileName}");
                    }

                    fileMappings[$"/temp/{fileName}"] = $"/blog/{(fileExtension == ".gif" ? "gif" : "img")}/{fileName}";
                }

                string updatedContent = ProcessContentImages(model.Content, fileMappings);
                string updatedContentUS = ProcessContentImages(model.ContentUS, fileMappings);
                string updatedContentTR = ProcessContentImages(model.ContentTR, fileMappings);
                string updatedContentDE = ProcessContentImages(model.ContentDE, fileMappings);

                string coverFileName = null!;
                if (model.ImageFile != null && model.ImageFile.Length > 0)
                {
                    coverFileName = $"{model.Url}_cover{Path.GetExtension(model.ImageFile.FileName)}";
                    string coverFilePath = Path.Combine(coverPath, coverFileName);

                    using var stream = new FileStream(coverFilePath, FileMode.Create);
                    await model.ImageFile.CopyToAsync(stream);
                    Console.WriteLine($"[DEBUG] Saved cover image: {coverFileName}");
                }

                // Create and populate the Blog entity.
                var blog = new Blog
                {
                    Title = model.Title,
                    Content = updatedContent,
                    Url = model.Url,
                    Image = coverFileName,
                    Date = DateTime.Now,
                    Author = model.Author,
                    RawYT = model.RawYT,
                    RawMaps = model.RawMaps,
                    CategoryId = model.SelectedCategoryIds?.FirstOrDefault(),
                    // Set whether this blog should appear on the home page.
                    isHome = model.IsHome
                };

                // If any quizzes were selected, assign them.
                if (model.SelectedQuizIds?.Any() == true)
                {
                    var quizzesToAdd = await _unitOfWork.Quizzes.GetAllAsync();
                    blog.Quiz = quizzesToAdd.Where(q => model.SelectedQuizIds.Contains(q.Id)).ToList();
                }

                // If any tags were selected, assign them.
                if (model.SelectedTagIds?.Any() == true)
                {
                    var tagsToAdd = await _unitOfWork.Tags.GetAllAsync();
                    blog.Tags = tagsToAdd.Where(t => model.SelectedTagIds.Contains(t.TagId)).ToList();
                }

                // Optionally assign the chosen quiz question to the blog.
                if (model.SelectedQuestionId.HasValue)
                {
                    blog.SelectedQuestionId = model.SelectedQuestionId.Value;
                }

                await _unitOfWork.Blogs.AddAsync(blog);
                await _unitOfWork.SaveAsync();

                // Save translations to resource files.
                SaveContentToResx(new BlogCreateModel
                {
                    TitleUS = model.TitleUS,
                    ContentUS = updatedContentUS,
                    TitleTR = model.TitleTR,
                    ContentTR = updatedContentTR,
                    TitleDE = model.TitleDE,
                    ContentDE = updatedContentDE,
                    Url = model.Url
                }, blog.BlogId);

                Console.WriteLine($"[DEBUG] Blog '{blog.Title}' saved successfully with ID {blog.BlogId}.");
                return RedirectToAction("Index");
            }

            #endregion
        #endregion
    }
}