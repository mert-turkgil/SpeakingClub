using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using SpeakingClub.Data.Abstract;
using SpeakingClub.Hubs;
using SpeakingClub.Models;

namespace SpeakingClub.Controllers
{
    [Authorize(Roles = "Root,Admin,Teacher")]
    [Route("[controller]")]
    public class QuizMonitorController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHubContext<QuizMonitorHub> _hubContext;
        private readonly ILogger<QuizMonitorController> _logger;

        public QuizMonitorController(
            ILogger<QuizMonitorController> logger, 
            IUnitOfWork unitOfWork, 
            IHubContext<QuizMonitorHub> hubContext)
        {
            _unitOfWork = unitOfWork;
            _hubContext = hubContext;
            _logger = logger;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            // Use repository method (NO Query()!)
            var submissions = await _unitOfWork.QuizSubmissions.GetAllWithIncludesAsync();

            var model = submissions.Select(s => new QuizMonitorViewModel
            {
                SubmissionId = s.QuizSubmissionId,
                UserName = s.User?.FirstName + " " + s.User?.LastName,
                QuizTitle = s.Quiz?.Title,
                Score = s.Score,
                MaxScore = 100,
                AttemptNumber = s.AttemptNumber,
                SubmissionDate = s.SubmissionDate,
                SubmissionTimeFormatted = s.SubmissionDate.ToString("g"),
                Age = s.User?.Age ?? 0,
                Responses = s.QuizResponses.Select(r => new UserResponseViewModel
                {
                    Question = r.QuizAnswer?.Question?.QuestionText ?? "", // null check
                    Answer = r.QuizAnswer?.AnswerText ?? r.AnswerText,
                    IsCorrect = r.QuizAnswer?.IsCorrect ?? false,
                    TimeTakenSeconds = r.TimeTakenSeconds
                }).ToList()
            }).ToList();

            return View(model); // Views/QuizMonitor/Index.cshtml
        }

        [HttpGet("GetSubmissions")]
        public async Task<IActionResult> GetSubmissions()
        {
            var submissions = await _unitOfWork.QuizSubmissions.GetAllWithIncludesAsync();

            var data = submissions.Select(s => new
            {
                SubmissionId = s.QuizSubmissionId,
                UserName = s.User?.FirstName + " " + s.User?.LastName,
                QuizTitle = s.Quiz?.Title,
                Score = (s.Quiz == null || s.Quiz.Questions == null || s.Quiz.Questions.Count == 0) ? 0 :
                 (int)Math.Round(s.Score * 100.0 / s.Quiz.Questions.Count),
                MaxScore = 100,
                AttemptNumber = s.AttemptNumber,
                SubmissionDate = s.SubmissionDate,
                SubmissionTimeFormatted = s.SubmissionDate.ToString("g"),
                Age = s.User?.Age ?? 0
            }).ToList();

            return Json(data);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View("Error!");
        }
    }
}
