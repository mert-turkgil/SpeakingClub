using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using OfficeOpenXml;
using SpeakingClub.Data.Abstract;
using SpeakingClub.Hubs;
using SpeakingClub.Identity;
using SpeakingClub.Models;

namespace SpeakingClub.Controllers
{
    [Authorize(Roles = "Root,Admin,Teacher")]
    [Route("[controller]")]
    public class QuizMonitorController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHubContext<QuizMonitorHub> _hubContext;
        private readonly ILogger<QuizMonitorController> _logger;

        public QuizMonitorController(
            ILogger<QuizMonitorController> logger,
            IUnitOfWork unitOfWork,
            UserManager<User> userManager,
            IHubContext<QuizMonitorHub> hubContext)
        {
            _unitOfWork = unitOfWork;
            _hubContext = hubContext;
            _logger = logger;
            _userManager = userManager;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            var submissions = await _unitOfWork.QuizSubmissions.GetAllWithIncludesAsync();

            // 1. Tüm UserId’leri topla
            var userIds = submissions.Select(s => s.UserId).Distinct().ToList();

            // 2. Userları UserManager üzerinden çek
            var users = _userManager.Users.Where(u => userIds.Contains(u.Id)).ToList();
            var userDict = users.ToDictionary(u => u.Id);

            // 3. ViewModel oluştur
            var model = submissions.Select(s => new QuizMonitorViewModel
            {
                SubmissionId = s.QuizSubmissionId,
                UserName = userDict.ContainsKey(s.UserId) 
                    ? userDict[s.UserId].FirstName + " " + userDict[s.UserId].LastName
                    : "Unknown",
                QuizTitle = s.Quiz?.Title,
                Score = s.Score,
                MaxScore = 100,
                AttemptNumber = s.AttemptNumber,
                SubmissionDate = s.SubmissionDate,
                SubmissionTimeFormatted = s.SubmissionDate.ToString("g"),
                Age = userDict.ContainsKey(s.UserId) ? userDict[s.UserId].Age : 0,
                Responses = s.QuizResponses.Select(r => new UserResponseViewModel
                {
                    Question = r.QuizAnswer?.Question?.QuestionText ?? "",
                    Answer = r.QuizAnswer?.AnswerText ?? r.AnswerText,
                    IsCorrect = r.QuizAnswer?.IsCorrect ?? false,
                    TimeTakenSeconds = r.TimeTakenSeconds
                }).ToList()
            }).ToList();

            return View(model);
        }

        [HttpGet("GetSubmissions")]
        public async Task<IActionResult> GetSubmissions()
        {
            var submissions = await _unitOfWork.QuizSubmissions.GetAllWithIncludesAsync();

            var userIds = submissions.Select(s => s.UserId).Distinct().ToList();
            var users = _userManager.Users.Where(u => userIds.Contains(u.Id)).ToList();
            var userDict = users.ToDictionary(u => u.Id);

            var data = submissions.Select(s => new
            {
                SubmissionId = s.QuizSubmissionId,
                UserName = userDict.ContainsKey(s.UserId)
                    ? userDict[s.UserId].FirstName + " " + userDict[s.UserId].LastName
                    : "Unknown",
                QuizTitle = s.Quiz?.Title,
                Score = (s.Quiz == null || s.Quiz.Questions == null || s.Quiz.Questions.Count == 0) ? 0 :
                    (int)Math.Round(s.Score * 100.0 / s.Quiz.Questions.Count),
                MaxScore = 100,
                AttemptNumber = s.AttemptNumber,
                SubmissionDate = s.SubmissionDate,
                SubmissionTimeFormatted = s.SubmissionDate.ToString("g"),
                Age = userDict.ContainsKey(s.UserId) ? userDict[s.UserId].Age : 0
            }).ToList();

            return Json(data);
        }
        
        [Route("ExportExcel")]
        [HttpGet]
        public async Task<IActionResult> ExportExcel()
        {
            var submissions = await _unitOfWork.QuizSubmissions.GetAllWithIncludesAsync();

            var userIds = submissions.Select(s => s.UserId).Distinct().ToList();
            var users = _userManager.Users.Where(u => userIds.Contains(u.Id)).ToList();
            var userDict = users.ToDictionary(u => u.Id);

            using var package = new ExcelPackage();
            var ws = package.Workbook.Worksheets.Add("QuizSubmissions");
            ws.Cells["A1"].Value = "Kullanıcı";
            ws.Cells["B1"].Value = "Quiz";
            ws.Cells["C1"].Value = "Puan";
            ws.Cells["D1"].Value = "Deneme";
            ws.Cells["E1"].Value = "Tarih";

            int row = 2;
            foreach (var sub in submissions)
            {
                ws.Cells[row, 1].Value = userDict.ContainsKey(sub.UserId)
                    ? userDict[sub.UserId].FirstName + " " + userDict[sub.UserId].LastName
                    : "Unknown";
                ws.Cells[row, 2].Value = sub.Quiz?.Title;
                ws.Cells[row, 3].Value = sub.Score;
                ws.Cells[row, 4].Value = sub.AttemptNumber;
                ws.Cells[row, 5].Value = sub.SubmissionDate.ToString("g");
                row++;
            }

            return File(package.GetAsByteArray(),
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        "QuizSubmissions.xlsx");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View("Error!");
        }
    }
}
