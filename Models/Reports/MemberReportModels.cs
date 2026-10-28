using System;
using System.Collections.Generic;
using SpeakingClub.Services.Email;
using SpeakingClub.Services.Reports;

namespace SpeakingClub.Models.Reports
{
    // Admin → Kullanıcılar (AdminController.Members.cs, Services/Reports/MemberReportService.cs)

    /// <summary>Headline figures of the filtered members (counts; the page turns them into shares).</summary>
    public record MemberKpis(int Members, int New30, int Confirmed, int Activated, int TwoPlus, int Passed, int Active30, int PromoIn)
    {
        public static readonly MemberKpis Empty = new(0, 0, 0, 0, 0, 0, 0, 0);
    }

    /// <summary>One member in the list. Dates are Turkish time; null when unknown.</summary>
    public record MemberRow(string Id, string Name, string? Email, string? UserName, DateTime? CreatedLocal, DateTime? LastLoginLocal,
        bool EmailConfirmed, bool Locked, bool Google, IReadOnlyList<string> Roles, int Attempts, int Quizzes, double? AverageScore,
        int? BestScore, DateTime? LastExamLocal, bool PromoEmails)
    {
        public bool IsStaff => Roles.Contains("Root") || Roles.Contains("Admin") || Roles.Contains("Teacher");
    }

    /// <summary>What the signed-in admin may do with a member (Root accounts are untouchable; Admins only by Root).</summary>
    public record MemberPermissions(bool IsSelf, bool CanEdit, bool CanDelete);

    public class MemberReportPage
    {
        public MemberReportFilter Filter { get; init; } = new();
        public MemberKpis Kpis { get; init; } = MemberKpis.Empty;
        public IReadOnlyList<MemberRow> Rows { get; init; } = Array.Empty<MemberRow>();
        public IReadOnlyDictionary<string, MemberPermissions> Permissions { get; init; } = new Dictionary<string, MemberPermissions>();
        public int TotalRows { get; init; }
        public int TotalPages => Math.Max(1, (int)Math.Ceiling(TotalRows / (double)Filter.Size));
    }

    public record MemberExam(int SubmissionId, int QuizId, string QuizTitle, string? Level, DateTime Local, int Attempt, int Score,
        int DurationSeconds, int Violations, string EndReason);

    public record MemberDownload(string FileName, DateTime Local);

    /// <summary>One member's page (Admin/Users/{id}): account, e-mail consent, exams and downloads.</summary>
    public class MemberDetail
    {
        public MemberRow Member { get; init; } = null!;
        public MemberPermissions Permissions { get; set; } = new(false, false, false);   // set by the controller (who is looking)
        public string? PreferredCulture { get; init; }
        public EmailTopics EmailOptOuts { get; init; }
        public DateTime? PromoConsentLocal { get; init; }
        public string? PromoConsentSource { get; init; }
        public DateTimeOffset? LockoutEnd { get; init; }
        public IReadOnlyList<MemberExam> Exams { get; init; } = Array.Empty<MemberExam>();
        public IReadOnlyList<MemberDownload> Downloads { get; init; } = Array.Empty<MemberDownload>();
        public int DownloadCount { get; init; }
        /// <summary>The highest level (A1…C2) of a quiz this member passed, if any.</summary>
        public string? HighestPassedLevel { get; init; }
    }
}
