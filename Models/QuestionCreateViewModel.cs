using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace SpeakingClub.Models
{
    public class QuestionCreateViewModel
    {
        #nullable disable
        public string QuestionText { get; set; }
        public IFormFile ImageFile { get; set; }
        public string ImageUrl { get; set; }
        public IFormFile AudioFile { get; set; }
        public string AudioUrl { get; set; }
        public string VideoUrl { get; set; }
        public int? QuizId { get; set; }
        public IEnumerable<SelectListItem> AvailableQuizzes { get; set; }
        public List<AnswerEditViewModel> Answers { get; set; } = new List<AnswerEditViewModel>
        {
            new AnswerEditViewModel(), new AnswerEditViewModel()
        };
    }
}