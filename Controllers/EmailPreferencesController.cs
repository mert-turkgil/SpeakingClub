using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SpeakingClub.Identity;
using SpeakingClub.Services.Email;

namespace SpeakingClub.Controllers
{
    /// <summary>
    /// The link in every announcement e-mail: /eposta/{token} (Turkish) or /e-mail/{token} (German). No sign-in, the
    /// random token is the key. The same address takes Gmail's and Yahoo's one-click unsubscribe (RFC 8058: a POST
    /// with "List-Unsubscribe=One-Click"), which carries no antiforgery token. Exam results are not affected.
    /// </summary>
    public class EmailPreferencesController : Controller
    {
        private readonly ApplicationDbContext _identity;

        public EmailPreferencesController(ApplicationDbContext identity) => _identity = identity;

        public class PreferencesPage
        {
            public string Token { get; init; } = "";
            public string? Name { get; init; }
            public string? Email { get; init; }
            public bool NewPosts { get; init; }
            public bool NewQuizzes { get; init; }
            public bool Saved { get; init; }
            public bool Found { get; init; } = true;
        }

        private Task<User?> FindAsync(string token) =>
            string.IsNullOrWhiteSpace(token) || token.Length > 64
                ? Task.FromResult<User?>(null)
                : _identity.Users.FirstOrDefaultAsync(u => u.EmailPreferencesToken == token);

        [HttpGet("eposta/{token}")]
        [HttpGet("e-mail/{token}")]
        public async Task<IActionResult> Index(string token, bool saved = false)
        {
            ViewData["Robots"] = "noindex, nofollow";
            var user = await FindAsync(token);
            if (user == null) return View(new PreferencesPage { Found = false });
            return View(Page(user, token, saved));
        }

        [HttpPost("eposta/{token}")]
        [HttpPost("e-mail/{token}")]
        [IgnoreAntiforgeryToken]   // one-click unsubscribe comes from the mail provider, without a form token
        public async Task<IActionResult> Save(string token, bool newPosts, bool newQuizzes, string? all)
        {
            var oneClick = Request.HasFormContentType && Request.Form["List-Unsubscribe"] == "One-Click";
            var user = await FindAsync(token);
            if (user == null) return oneClick ? Ok() : RedirectToAction(nameof(Index), new { token });

            user.EmailOptOuts = oneClick || all != null
                ? EmailTopics.All
                : (newPosts ? EmailTopics.None : EmailTopics.NewPosts) | (newQuizzes ? EmailTopics.None : EmailTopics.NewQuizzes);
            user.PromoConsentAt = DateTime.UtcNow;
            user.PromoConsentSource = oneClick || all != null ? ConsentSource.Unsubscribe : ConsentSource.Account;
            await _identity.SaveChangesAsync();
            if (oneClick) return Ok();
            return Redirect($"{Request.Path}?saved=true");
        }

        private static PreferencesPage Page(User user, string token, bool saved) => new()
        {
            Token = token,
            Name = user.FirstName,
            Email = Mask(user.Email),
            NewPosts = !user.EmailOptOuts.HasFlag(EmailTopics.NewPosts),
            NewQuizzes = !user.EmailOptOuts.HasFlag(EmailTopics.NewQuizzes),
            Saved = saved
        };

        // "su***@gmail.com": enough to recognise one's own address, not enough to read someone else's from a forwarded link
        private static string? Mask(string? email)
        {
            if (string.IsNullOrEmpty(email)) return null;
            var at = email.IndexOf('@');
            return at <= 2 ? email : email[..2] + new string('*', Math.Min(5, at - 2)) + email[at..];
        }
    }
}
