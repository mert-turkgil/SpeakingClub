using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpeakingClub.Models.Reports;
using SpeakingClub.Services.Reports;

namespace SpeakingClub.Controllers
{
    /// <summary>
    /// Admin → Sınav sonuçları. The page is rendered on the server with only the current filter's page of results
    /// (it used to load every submission with every answer and filter in the browser). The analysis tabs load their
    /// figures as JSON when opened (Stats); one attempt opens in a side panel (Attempt). Teachers see it too.
    /// </summary>
    [Authorize(Roles = "Root,Admin,Teacher")]
    [Route("[controller]")]
    public class QuizMonitorController : Controller
    {
        private readonly ExamReportService _reports;

        public QuizMonitorController(ExamReportService reports) => _reports = reports;

        private static bool IsGerman => CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "de";
        private static string T(string tr, string de) => IsGerman ? de : tr;

        [HttpGet("")]
        public async Task<IActionResult> Index([FromQuery] ExamReportFilter filter)
        {
            filter.Normalize();
            var (from, to) = filter.Range();
            var previous = filter.PreviousRange();
            var (rows, total) = await _reports.ResultsAsync(filter);
            var model = new ExamReportPage
            {
                Filter = filter,
                Kpis = await _reports.KpisAsync(filter, from, to),
                Previous = previous is { } p ? await _reports.KpisAsync(filter, p.From, p.To) : null,
                Rows = rows,
                TotalRows = total,
                Quizzes = await _reports.QuizOptionsAsync(),
                Categories = await _reports.CategoryOptionsAsync(),
                ActiveNow = await _reports.ActiveNowAsync()
            };
            return View(model);
        }

        /// <summary>One attempt: the side panel's content (fetch) or a page of its own (opened in a new tab).</summary>
        [HttpGet("attempt/{id:int}")]
        public async Task<IActionResult> Attempt(int id, bool panel = false)
        {
            var detail = await _reports.AttemptAsync(id);
            if (detail == null) return NotFound();
            return panel ? PartialView("_Attempt", detail) : View("Attempt", detail);
        }

        /// <summary>Figures for one analysis tab, as JSON (wwwroot/js/admin-reports.js draws them).</summary>
        [HttpGet("stats/{tab}")]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> Stats(string tab, [FromQuery] ExamReportFilter filter, string? mode, bool effort = false)
        {
            filter.Normalize();
            switch (tab)
            {
                case "overview": return Json(await _reports.OverviewAsync(filter));
                case "activity": return Json(await _reports.ActivityAsync(filter));
                case "progress": return Json(await _reports.ProgressAsync(filter));
                case "integrity": return Json(await _reports.IntegrityAsync(filter));
                case "items":
                    var items = await _reports.ItemsAsync(filter, mode, effort);
                    if (items is not { } found) return Json(new { needsQuiz = true });
                    return Json(new
                    {
                        found.Result.Attempts, found.Result.Excluded, found.Result.Questions, found.Result.MeanPercent, found.Result.MedianPercent,
                        found.Result.SdPercent, found.Result.Kr20, found.Result.SemPercent, found.Result.LowEffortShare, found.Result.Items,
                        minAttempts = ItemAnalysis.MinAttemptsForStatistics,
                        rows = found.Result.Rows.OrderByDescending(r => r.Total).Take(300).Select(r => new
                        {
                            r.SubmissionId, name = found.Names.GetValueOrDefault(r.UserId, ""), r.Total, r.Cells, r.GuttmanErrors,
                            r.Rte, r.RapidAnswers, r.TimedAnswers
                        })
                    });
                default:
                    return NotFound();
            }
        }

        /// <summary>
        /// The filtered results as a CSV file for Excel (ReportCsv), dates in Turkish time. Streamed, so a long list
        /// doesn't sit in memory.
        /// </summary>
        [HttpGet("export.csv")]
        public async Task ExportCsv([FromQuery] ExamReportFilter filter)
        {
            filter.Normalize();
            await using var writer = ReportCsv.Start(Response, T("sinav-sonuclari", "pruefungsergebnisse"));
            await ReportCsv.WriteRowAsync(writer, new[]
            {
                T("Tarih", "Datum"), T("Öğrenci", "Lernende"), T("E-posta", "E-Mail"), T("Sınav", "Prüfung"), T("Seviye", "Niveau"),
                T("Deneme", "Versuch"), T("Puan (%)", "Punkte (%)"), T("Doğru", "Richtig"), T("Cevaplanan", "Beantwortet"),
                T("Soru", "Fragen"), T("Süre (sn)", "Dauer (s)"), T("Uyarı", "Warnungen"), T("Nasıl bitti", "Ende")
            });
            await foreach (var r in _reports.ExportAsync(filter))
            {
                await ReportCsv.WriteRowAsync(writer, new[]
                {
                    r.SubmittedLocal.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture), r.StudentName, r.Email ?? "", r.QuizTitle,
                    r.Level ?? "", r.Attempt.ToString(CultureInfo.InvariantCulture), r.Score.ToString(CultureInfo.InvariantCulture),
                    r.Correct.ToString(CultureInfo.InvariantCulture), r.Answered.ToString(CultureInfo.InvariantCulture),
                    r.Total.ToString(CultureInfo.InvariantCulture), r.DurationSeconds.ToString(CultureInfo.InvariantCulture),
                    r.Violations.ToString(CultureInfo.InvariantCulture), EndLabel(r.EndReason)
                });
            }
        }

        private static string EndLabel(string end) => end switch
        {
            "timeout" => T("Süre doldu", "Zeit abgelaufen"),
            "violations" => T("Kural ihlaliyle bitti", "Wegen Verstößen beendet"),
            _ => T("Gönderildi", "Abgegeben")
        };
    }
}
