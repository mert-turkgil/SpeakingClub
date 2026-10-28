using System;
using System.Collections.Generic;
using SpeakingClub.Services.Reports;

namespace SpeakingClub.Models.Reports
{
    // Admin → Sınav sonuçları (QuizMonitorController, Services/Reports/ExamReportService.cs)

    public record ReportQuizOption(int Id, string Title, string? TitleDe, string? Level, int? CategoryId, int Questions, int? TimeLimitMinutes);
    public record ReportCategoryOption(int Id, string Name);

    /// <summary>Headline figures of a period. Rates are 0…1; durations in seconds.</summary>
    public record ExamKpis(int Attempts, int Students, double? Mean, double? Median, double? PassRate, double? MedianDuration,
        double? WarningRate, double? TimeoutRate)
    {
        public static readonly ExamKpis Empty = new(0, 0, null, null, null, null, null, null);
    }

    public record ExamResultRow(int SubmissionId, string UserId, string StudentName, string? Email, int QuizId, string QuizTitle,
        string? Level, DateTime SubmittedLocal, int Attempt, int Score, int Correct, int Answered, int Total, int DurationSeconds,
        int? TimeLimitSeconds, int Violations, string EndReason);

    public class ExamReportPage
    {
        public ExamReportFilter Filter { get; init; } = new();
        public ExamKpis Kpis { get; init; } = ExamKpis.Empty;
        public ExamKpis? Previous { get; init; }
        public IReadOnlyList<ExamResultRow> Rows { get; init; } = Array.Empty<ExamResultRow>();
        public int TotalRows { get; init; }
        public int TotalPages => Math.Max(1, (int)Math.Ceiling(TotalRows / (double)Filter.Size));
        public IReadOnlyList<ReportQuizOption> Quizzes { get; init; } = Array.Empty<ReportQuizOption>();
        public IReadOnlyList<ReportCategoryOption> Categories { get; init; } = Array.Empty<ReportCategoryOption>();
        public int ActiveNow { get; init; }
        public string? StudentLabel { get; init; }
    }

    /// <summary>One attempt in detail (the drawer that opens from a result row).</summary>
    public record AttemptDetail(int SubmissionId, string UserId, string StudentName, string? Email, int QuizId, string QuizTitle,
        string? Level, DateTime SubmittedLocal, DateTime? StartedLocal, int Attempt, int Score, int DurationSeconds, int? TimeLimitSeconds,
        int Violations, string EndReason, bool TimesRecorded, IReadOnlyList<AttemptAnswer> Answers, IReadOnlyList<AttemptEvent> Events,
        double? Rte);

    /// <param name="ClassP">Share of all students who answered this question correctly</param>
    public record AttemptAnswer(int Position, int? QuestionId, string Question, string? Chosen, IReadOnlyList<string> CorrectAnswers,
        bool? IsCorrect, bool Skipped, int Seconds, double? ClassMeanSeconds, bool Rapid, double? ClassP);

    public record AttemptEvent(string Type, int SecondsFromStart, bool Violation);
}
