using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpeakingClub.Models.Reports;
using SpeakingClub.Services.Reports;

namespace SpeakingClub.Controllers
{
    // Admin → Kullanıcılar: the member report (Services/Reports/MemberReportService.cs) and one member's page.
    // Admin and Root only. Creating, editing and deleting accounts stay in AdminController.cs ("Hesap").
    public partial class AdminController
    {
        [HttpGet("Users"), Authorize(Roles = "Admin,Root")]
        public async Task<IActionResult> Users([FromQuery] MemberReportFilter filter, [FromServices] MemberReportService members)
        {
            filter.Normalize();
            var (rows, total) = await members.PageAsync(filter);
            var me = _userManager.GetUserId(User) ?? "";
            var isRoot = User.IsInRole("Root");
            return View("Members", new MemberReportPage
            {
                Filter = filter,
                Kpis = await members.KpisAsync(filter),
                Rows = rows,
                TotalRows = total,
                Permissions = rows.ToDictionary(r => r.Id, r => MemberReportService.PermissionsFor(r, me, isRoot))
            });
        }

        [HttpGet("Users/{id}"), Authorize(Roles = "Admin,Root")]
        public async Task<IActionResult> Member(string id, [FromServices] MemberReportService members)
        {
            var detail = await members.DetailAsync(id);
            if (detail == null)
            {
                TempData["ErrorMessage"] = T("Kullanıcı bulunamadı.", "Benutzer nicht gefunden.");
                return RedirectToAction(nameof(Users));
            }
            detail.Permissions = MemberReportService.PermissionsFor(detail.Member, _userManager.GetUserId(User) ?? "", User.IsInRole("Root"));
            return View("Member", detail);
        }

        /// <summary>Figures for one chart tab of the member report, as JSON (wwwroot/js/admin-reports.js draws them).</summary>
        [HttpGet("Users/stats/{tab}"), Authorize(Roles = "Admin,Root")]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> UserStats(string tab, [FromQuery] MemberReportFilter filter, [FromServices] MemberReportService members)
        {
            filter.Normalize();
            return tab switch
            {
                "growth" => Json(await members.GrowthAsync(filter)),
                "cohort" => Json(await members.CohortAsync(filter)),
                _ => NotFound()
            };
        }

        /// <summary>The filtered members as a CSV file for Excel (dates in Turkish time).</summary>
        [HttpGet("Users/export.csv"), Authorize(Roles = "Admin,Root")]
        public async Task UsersExport([FromQuery] MemberReportFilter filter, [FromServices] MemberReportService members)
        {
            filter.Normalize();
            await using var writer = ReportCsv.Start(Response, T("kullanicilar", "benutzer"));
            await ReportCsv.WriteRowAsync(writer,
                T("Ad", "Name"), T("E-posta", "E-Mail"), T("Kayıt", "Registriert"), T("Son giriş", "Letzte Anmeldung"), T("Rol", "Rolle"),
                T("E-posta onaylı", "E-Mail bestätigt"), T("Kilitli", "Gesperrt"), "Google", T("Deneme", "Versuche"), T("Sınav", "Prüfungen"),
                T("Ortalama puan", "Durchschnitt"), T("En iyi puan", "Bestwert"), T("Son sınav", "Letzte Prüfung"), T("Duyuru e-postaları", "Ankündigungen per E-Mail"));
            string Date(System.DateTime? d) => d?.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture) ?? "";
            string YesNo(bool b) => b ? T("evet", "ja") : T("hayır", "nein");
            await foreach (var m in members.ExportAsync(filter))
            {
                await ReportCsv.WriteRowAsync(writer,
                    m.Name, m.Email ?? "", Date(m.CreatedLocal), Date(m.LastLoginLocal), string.Join(", ", m.Roles),
                    YesNo(m.EmailConfirmed), YesNo(m.Locked), YesNo(m.Google), m.Attempts.ToString(CultureInfo.InvariantCulture),
                    m.Quizzes.ToString(CultureInfo.InvariantCulture), m.AverageScore?.ToString("0.0", CultureInfo.CurrentCulture) ?? "",
                    m.BestScore?.ToString(CultureInfo.InvariantCulture) ?? "", Date(m.LastExamLocal), YesNo(m.PromoEmails));
            }
        }
    }
}
