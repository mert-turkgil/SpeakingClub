using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace SpeakingClub.Models
{
    // What Views/Admin/_QuizForm.cshtml needs, filled from either QuizCreateViewModel or QuizEditViewModel.
    // Both models post the same field names (Questions[i].Answers[j].…), so one form serves both pages.
    public record QuizFormAnswer(int AnswerId, string Text, bool IsCorrect);

    public record QuizFormQuestion(int QuestionId, string Text, string? ImageUrl, string? AudioUrl, string? VideoUrl, List<QuizFormAnswer> Answers);

    public class QuizFormData
    {
        public bool IsEdit { get; init; }
        public int QuizId { get; init; }
        public string? Title { get; init; }
        public string? Description { get; init; }
        public bool DescriptionRequired { get; init; }
        public int? CategoryId { get; init; }
        public int? TimeLimitMinutes { get; init; }
        public string? ImageUrl { get; init; }
        public string? AudioUrl { get; init; }
        public string? YouTubeVideoUrl { get; init; }
        public string? TitleDe { get; init; }
        public string? DescriptionDe { get; init; }
        public string? Slug { get; init; }
        public string? SlugDe { get; init; }
        public string? MetaDescription { get; init; }
        public string? MetaDescriptionDe { get; init; }
        public string? MetaKeywords { get; init; }
        public string? MetaKeywordsDe { get; init; }
        public string? TeacherId { get; init; }
        public bool IsAnonymous { get; init; }
        public List<int> SelectedTagIds { get; init; } = new();
        public List<int> SelectedWordIds { get; init; } = new();
        public IEnumerable<SelectListItem> Categories { get; init; } = Enumerable.Empty<SelectListItem>();
        public IEnumerable<SelectListItem> Tags { get; init; } = Enumerable.Empty<SelectListItem>();
        public IEnumerable<SelectListItem> Words { get; init; } = Enumerable.Empty<SelectListItem>();
        public IEnumerable<SelectListItem> TeacherOptions { get; init; } = Enumerable.Empty<SelectListItem>();
        public List<QuizFormQuestion> Questions { get; init; } = new();

        public static QuizFormData From(QuizCreateViewModel m) => new()
        {
            IsEdit = false,
            Title = m.Title,
            Description = m.Description,
            DescriptionRequired = true,
            CategoryId = m.CategoryId,
            TimeLimitMinutes = m.TimeLimitMinutes,
            YouTubeVideoUrl = m.YouTubeVideoUrl,
            TitleDe = m.TitleDe,
            DescriptionDe = m.DescriptionDe,
            Slug = m.Slug,
            SlugDe = m.SlugDe,
            MetaDescription = m.MetaDescription,
            MetaDescriptionDe = m.MetaDescriptionDe,
            MetaKeywords = m.MetaKeywords,
            MetaKeywordsDe = m.MetaKeywordsDe,
            TeacherId = m.TeacherId,
            IsAnonymous = m.IsAnonymous,
            SelectedTagIds = m.SelectedTagIds ?? new(),
            SelectedWordIds = m.SelectedWordIds ?? new(),
            Categories = m.Categories ?? Enumerable.Empty<SelectListItem>(),
            Tags = m.Tags ?? Enumerable.Empty<SelectListItem>(),
            Words = m.Words ?? Enumerable.Empty<SelectListItem>(),
            TeacherOptions = m.TeacherOptions,
            Questions = (m.Questions ?? new()).Select(q => new QuizFormQuestion(0, q.QuestionText, null, null, q.VideoUrl,
                (q.Answers ?? new()).Select(a => new QuizFormAnswer(0, a.AnswerText ?? string.Empty, a.IsCorrect)).ToList())).ToList()
        };

        public static QuizFormData From(QuizEditViewModel m) => new()
        {
            IsEdit = true,
            QuizId = m.QuizId,
            Title = m.Title,
            Description = m.Description,
            CategoryId = m.CategoryId,
            TimeLimitMinutes = m.TimeLimitMinutes,
            ImageUrl = m.ImageUrl,
            AudioUrl = m.AudioUrl,
            YouTubeVideoUrl = m.YouTubeVideoUrl,
            TitleDe = m.TitleDe,
            DescriptionDe = m.DescriptionDe,
            Slug = m.Slug,
            SlugDe = m.SlugDe,
            MetaDescription = m.MetaDescription,
            MetaDescriptionDe = m.MetaDescriptionDe,
            MetaKeywords = m.MetaKeywords,
            MetaKeywordsDe = m.MetaKeywordsDe,
            TeacherId = m.TeacherId,
            IsAnonymous = m.IsAnonymous,
            SelectedTagIds = m.SelectedTagIds ?? new(),
            SelectedWordIds = m.SelectedWordIds ?? new(),
            Categories = m.Categories,
            Tags = m.Tags,
            Words = m.Words,
            TeacherOptions = m.TeacherOptions,
            Questions = (m.Questions ?? new()).Select(q => new QuizFormQuestion(q.QuestionId, q.QuestionText ?? string.Empty, q.ImageUrl, q.AudioUrl, q.VideoUrl,
                (q.Answers ?? new()).Select(a => new QuizFormAnswer(a.AnswerId, a.AnswerText ?? string.Empty, a.IsCorrect == "true")).ToList())).ToList()
        };
    }
}
