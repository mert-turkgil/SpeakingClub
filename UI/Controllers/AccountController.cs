using System.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UI.Entity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using UI.EmailServices;
using UI.Extensions;
using UI.Identity;
using UI.Models;
using System.Security.Claims;
using WebUi.Models;

namespace UI.Controllers
{   
    [AutoValidateAntiforgeryToken]
    [Route("account/")]
    public class AccountController:Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;


        private readonly IEmailSender _emailSender;

        private readonly IWebHostEnvironment _webHostEnvironment;
        
        public AccountController(UserManager<User> userManager,
        SignInManager<User> signInManager,
        IEmailSender emailSender,
        IWebHostEnvironment webHostEnvironment
        )
        {
            _userManager = userManager;
            _signInManager =signInManager;
            _emailSender = emailSender;
            _webHostEnvironment = webHostEnvironment;
        }

        [AllowAnonymous]
        [HttpGet("externallogin")]
        public IActionResult ExternalLogin(string provider, string returnUrl = "http://localhost:5175")
        {
            // Request a redirect to the external login provider
            var redirectUrl = Url.Action(nameof(ExternalLoginCallback), "Account", new { ReturnUrl = returnUrl });
            var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
            return Challenge(properties, provider);
        }

        private IActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            else
            {
                return RedirectToAction(nameof(HomeController.Index), "Home");
            }
        }


        [AllowAnonymous]
        [HttpGet("ExternalLoginCallback")]
        public async Task<IActionResult> ExternalLoginCallback(string returnUrl = "http://localhost:5175", string? remoteError = null)
        {
            // Handle the callback from the external login provider
            var info = await _signInManager.GetExternalLoginInfoAsync();

            if (info == null)
            {
                // Handle the error or redirect to the registration page
                return RedirectToAction(nameof(Register));
            }

            // Sign in the user with this external login provider if the user already has a login.
            var result = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor: true);

            if (result.Succeeded)
            {
                // User is successfully signed in
                return RedirectToLocal(returnUrl);
            }
            else
            {
                // If the user does not have an account, prompt the user to create an account
                ViewData["ReturnUrl"] = returnUrl;
                ViewData["LoginProvider"] = info.LoginProvider;
                return View("ExternalLoginConfirmation", new ExternalLoginConfirmationViewModel { Email = info.Principal.FindFirstValue(ClaimTypes.Email) });
            }
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("/login")]  
        public async Task<IActionResult> Login(LoginModel model){

            if (!ModelState.IsValid)
            {
                return View(model);
            }
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user==null)
            {
                ModelState.AddModelError("","Şifre ve ya Email hatalı");
                return View(model);
            }
            if (!await _userManager.IsEmailConfirmedAsync(user))
            {
                ModelState.AddModelError("","Lütfen Email hesabınıza gelen link ile hesabınızı onaylayın");
                return View(model);
            }
            var result = await _signInManager.PasswordSignInAsync(user,model.Password,false,false);
            if (result.Succeeded)
            {
                return Redirect(model.ReturnUrl??"~/");
            }

            ModelState.AddModelError("","Şifre ve ya Email hatalı");
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("/register")]
        public async Task<IActionResult> Register(RegisterModel model){
            if (!ModelState.IsValid)
            {
                return View(model);
            }
                var user = new User(){
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    UserName = model.UserName,
                    Email = model.Email,
                    
                };
            
            var result = await _userManager.CreateAsync(user,model.Password);
            if (result.Succeeded)
            {   
                await _userManager.AddToRoleAsync(user,"customer");
                //token oluşturma ve mail gönderme
                //token oluşturma
                var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                var url = Url.Action("ConfirmEmail","Account",new{
                    userId = user.Id,
                    token = code
                });
                //Email
                await _emailSender.SendEmailAsync(model.Email,
                //Mail başlığı
                "Potwierdź swoje konto.",
                $"Lütfen email hesabınızı onaylamak için linke <a href='https://localhost:7070{url}'>tıklayınız.</a>"
                );


                return RedirectToAction("Login","Account");
            }
            ModelState.AddModelError("RePassword","Şifreniz aynı olmalıdır!");
            ModelState.AddModelError("","Bilinmeyen bir hata oluştu");
            return View(model);
        }
        [HttpGet("/register")]
        public IActionResult Register(){
            return View();
        }
        [Route("logout")]
        public async Task<IActionResult>Logout(){

        TempData.Put("message",new AlertMessage(){
        Title="Wylogowany",
        Message="Do zobaczenia wkrótce ^^",
        AlertType="warning",
        icon="fa-regular",
        icon2="fa-face-grin-wink fa-flip"
        });
            await _signInManager.SignOutAsync();
            return Redirect("~/");
        }
        
        [Route("Account/ConfirmEmail")]
        public async Task<IActionResult> ConfirmEmail(string userId, string token)
        {
            if (userId == null || token == null)
            {
                TempData.Put("message", new AlertMessage
                {
                    Title = "Nieprawidłowy Token",
                    Message = "Token zabezpieczający witrynę wygasł lub nie został przetworzony!",
                    AlertType = "danger",
                    icon = "fa-solid",
                    icon2 = "fa-user-ninja fa-beat-fade"
                });
                return View();
            }

            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                TempData.Put("message", new AlertMessage
                {
                    Title = "Nie znaleziono użytkownika",
                    Message = "Żądany użytkownik nie został znaleziony, można skontaktować się z właścicielem witryny lub utworzyć nowego użytkownika.",
                    AlertType = "danger",
                    icon = "fa-solid",
                    icon2 = "fa-person-circle-question fa-beat-fade"
                });
                return View();
            }

            var result = await _userManager.ConfirmEmailAsync(user, token);

            if (result.Succeeded)
            {


                TempData.Put("message", new AlertMessage
                {
                    Title = "Twoje konto zostało zatwierdzone",
                    Message = "Można z powodzeniem rozpocząć zakupy :)",
                    AlertType = "success",
                    icon = "fa-regular",
                    icon2 = "fa-thumbs-up fa-beat-fade"
                });

                return View();
            }

            TempData.Put("message", new AlertMessage
            {
                Title = "Twoje konto nie zostało zatwierdzone!",
                Message = "Nie mogliśmy potwierdzić Twojego konta! Spróbuj ponownie.",
                AlertType = "warning",
                icon = "fa-solid",
                icon2 = "fa-skull-crossbones fa-beat-fade"
            });

            return View();
        }

        [HttpGet("/forgotpassword")]
        public IActionResult ForgotPassword(){
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("/forgotpassword")]
        public async Task<IActionResult> ForgotPassword(ForgotPassword model){
            if (string.IsNullOrEmpty(model.Email))
            {
                return View(model);
            }
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user==null)
            {
                return View(model);
            }
            var code = await _userManager.GeneratePasswordResetTokenAsync(user);
            var url = Url.Action("ResetPassword","Account",new{
                    userId = user.Id,
                    token = code
                });
             await _emailSender.SendEmailAsync(model.Email,
                //Mail başlığı
                "Hesabınızın Şifresini Değiştirin.",
                $"Lütfen email hesabınızın şifresini linke tıklayarak değiştirin <a href='https://localhost:7070{url}'>tıklayınız.</a>"
                );
            return View(model);
        }
        [HttpGet("/resetpassword")]
        public IActionResult ResetPassword(string userId,string token){
            if (userId ==null || token == null)
            {
                     TempData.Put("message",new AlertMessage(){
                    Title="Zmiana hasła nie została potwierdzona",
                    Message="Ta zmiana hasła nie została autoryzowana ze względów bezpieczeństwa Kod:1",
                    AlertType="danger",
                    icon="fa-solid",
                    icon2="fa-user-ninja fa-beat-fade"
                });
                return RedirectToAction("Home","Index");
            }
            var model = new ResetPasswordModel {Token=token};

            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("/resetpassword")]
        public async Task<IActionResult> ResetPassword(ResetPasswordModel model){
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user==null)
            {
                   TempData.Put("message",new AlertMessage(){
                    Title="Zmiana hasła nie została potwierdzona",
                    Message="Ta zmiana hasła nie została autoryzowana ze względów bezpieczeństwa Kod:2",
                    AlertType="danger",
                    icon="fa-solid",
                    icon2="fa-user-ninja fa-beat-fade"
                });
                return RedirectToAction("Home","Index");
            }
            var result = await _userManager.ResetPasswordAsync(user,model.Token,model.Password);
            if (result.Succeeded)
            {
                return RedirectToAction("Login", "Account");
            }
              TempData.Put("message",new AlertMessage(){
                    Title="Wystąpił błąd",
                    Message="Wystąpił błąd Error:404",
                    AlertType="danger",
                    icon="fa-solid",
                    icon2="fa-user-ninja fa-beat-fade"
                });
            return View(model);
        }

        public IActionResult AccessDenied(){
            return View();
        }
    }
}