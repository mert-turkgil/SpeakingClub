using System;
using System.Collections.Generic;

namespace SpeakingClub.Models
{
    // Admin home (Admin/Index): counts, this week's exam activity, the latest posts and the things
    // that need a look (quizzes without questions, drafts).
    public class AdminDashboardViewModel
    {
        public string FirstName { get; set; } = string.Empty;
        public bool IsAdmin { get; set; }

        public int BlogCount { get; set; }
        public int DraftBlogCount { get; set; }
        public int QuizCount { get; set; }
        public int QuestionCount { get; set; }
        public int WordCount { get; set; }
        public int SlideCount { get; set; }
        public int UserCount { get; set; }

        public int SubmissionsThisWeek { get; set; }
        public int? AverageScoreThisWeek { get; set; }

        public List<AdminRecentSubmission> RecentSubmissions { get; set; } = new();
        public List<AdminRecentBlog> RecentBlogs { get; set; } = new();
        public List<AdminQuizNotice> QuizzesWithoutQuestions { get; set; } = new();
    }

    public record AdminRecentSubmission(int SubmissionId, string StudentName, string QuizTitle, int Score, DateTime Date, string? EndReason);

    public record AdminRecentBlog(int BlogId, string Title, DateTime Date, bool IsPublished, string? Image);

    public record AdminQuizNotice(int QuizId, string Title);

    // Header of an admin page: breadcrumb, title, one-line explanation, the main button and the guide.
    public record AdminHeader(
        string SectionKey,
        string Title,
        string Lead,
        string? ActionHref = null,
        string? ActionLabel = null,
        string ActionIcon = "fa-solid fa-plus",
        AdminGuide? Guide = null,
        string? ParentTitle = null,
        string? ParentHref = null);
}
