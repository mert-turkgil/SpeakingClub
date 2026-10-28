using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using SpeakingClub.Services;
using SpeakingClub.Services.Email;

namespace SpeakingClub.Controllers
{
    /// <summary>
    /// /dev/emails: every e-mail template with sample content, in Turkish and German (Development only; 404 otherwise).
    /// For checking a template change without sending anything.
    /// </summary>
    [Route("dev/emails")]
    public class DevEmailsController : Controller
    {
        private readonly IWebHostEnvironment _env;
        private readonly EmailRenderer _renderer;

        public DevEmailsController(IWebHostEnvironment env, EmailRenderer renderer)
        {
            _env = env;
            _renderer = renderer;
        }

        [HttpGet("")]
        public IActionResult Index()
        {
            if (!_env.IsDevelopment()) return NotFound();
            var links = string.Concat(EmailTemplates.ModelTypes.Keys.Select(t =>
                $"<li>{t}: <a href=\"/dev/emails/{t}?culture=tr-TR\">Türkçe</a> · <a href=\"/dev/emails/{t}?culture=de-DE\">Deutsch</a></li>"));
            return Content($"<!doctype html><meta charset=\"utf-8\"><title>E-mail templates</title><body style=\"font-family:sans-serif\"><h1>E-mail templates</h1><ul>{links}</ul></body>", "text/html");
        }

        [HttpGet("{template}")]
        public async Task<IActionResult> Show(string template, string culture = UserLanguage.Turkish)
        {
            if (!_env.IsDevelopment() || !EmailTemplates.ModelTypes.ContainsKey(template)) return NotFound();
            var rendered = await _renderer.RenderAsync(template, Sample(template, culture), culture);
            var bar = $"<div style=\"font:13px sans-serif;padding:8px 12px;background:#2B2220;color:#fff\">Subject: <b>{WebUtility.HtmlEncode(rendered.Subject)}</b> · <a style=\"color:#FFD1D1\" href=\"/dev/emails\">all templates</a></div>";
            return Content(bar + rendered.Html, "text/html");
        }

        private static EmailModel Sample(string template, string culture)
        {
            var german = UserLanguage.IsGerman(culture);
            var site = SeoHelper.SiteUrl;
            EmailModel model = template switch
            {
                EmailTemplates.ExamResult => new ExamResultEmail
                {
                    SubmissionId = 1, QuizTitle = german ? "Deutsch A2 Grammatik" : "Almanca A2 dilbilgisi", Level = "A2", Score = 64, Correct = 9,
                    Total = 14, Answered = 13, DurationSeconds = 482, AttemptNumber = 2, PreviousBest = 50, PassScore = ExamRules.PassScore,
                    ReviewUrl = site + "/Account/ReviewQuiz/1?submission=1", QuizzesUrl = site + (german ? "/pruefungen" : "/sinavlar")
                },
                EmailTemplates.NewPost => new NewPostEmail
                {
                    Title = german ? "Die zehn häufigsten Fehler mit Präpositionen" : "Edatlarda en sık yapılan on hata",
                    Summary = german ? "Mit, bei oder zu? Eine kurze Übersicht mit Beispielen aus dem Alltag." : "Mit, bei mi zu mu? Günlük hayattan örneklerle kısa bir özet.",
                    ImageUrl = site + "/img/logo-512.png", Url = site + (german ? "/beitraege/beispiel" : "/yazilar/ornek"),
                    UnsubscribeUrl = site + "/eposta/ornek", PreferencesUrl = site + "/eposta/ornek", LegacyConsent = true
                },
                EmailTemplates.NewQuiz => new NewQuizEmail
                {
                    Title = german ? "B1 Hörverstehen" : "B1 dinleme", Summary = german ? "Fünf kurze Gespräche aus dem Alltag." : "Günlük hayattan beş kısa konuşma.",
                    Level = "B1", Questions = 15, TimeLimitMinutes = 20, Url = site + "/Account/StartQuiz/1",
                    UnsubscribeUrl = site + "/eposta/ornek", PreferencesUrl = site + "/eposta/ornek"
                },
                EmailTemplates.ConfirmEmail => new ConfirmEmailEmail { ConfirmUrl = site + "/Account/ConfirmEmail?userId=1&token=example" },
                EmailTemplates.ContactMessage => new ContactMessageEmail
                {
                    SenderName = "Ayşe Yılmaz", SenderEmail = "ayse@example.com", Language = culture,
                    Message = "Merhaba Suna Hanım,\nB1 kursu ne zaman başlıyor?\nTeşekkürler"
                },
                EmailTemplates.Test => new TestEmail { SentBy = "Admin" },
                _ => new ContactReceiptEmail()
            };
            model.Name = template == EmailTemplates.ContactMessage ? "Suna" : german ? "Anna" : "Ayşe";
            return model;
        }
    }
}
