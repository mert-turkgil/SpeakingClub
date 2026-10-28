using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace SpeakingClub.Services.Reports
{
    /// <summary>
    /// The filters of the exam report (Admin → Sınav sonuçları), read from the address bar so every view can be
    /// bookmarked, shared and reached with the back button. Property names are the query-string keys.
    /// </summary>
    public class ExamReportFilter
    {
        public static readonly string[] Periods = { "7", "30", "90", "365", "all", "custom" };
        public static readonly int[] PageSizes = { 25, 50, 100 };

        public string? Period { get; set; }
        public DateOnly? From { get; set; }
        public DateOnly? To { get; set; }
        public int? Quiz { get; set; }
        public string? Q { get; set; }          // student: part of the name or e-mail address
        public string? Level { get; set; }      // A1…C2 (quiz tag)
        public int? Category { get; set; }
        public string? Status { get; set; }     // pass | fail
        public string? End { get; set; }        // submitted | timeout | violations
        public bool First { get; set; }         // only each student's first attempt at a quiz
        public bool Warn { get; set; }          // only attempts with integrity warnings
        public string? Sort { get; set; }
        public int Page { get; set; } = 1;
        public int Size { get; set; } = 25;

        /// <summary>Clamps every value to what the page offers; anything unknown falls back to the default.</summary>
        public ExamReportFilter Normalize()
        {
            Period = Periods.Contains(Period) ? Period : "30";
            if (Period == "custom")
            {
                if (From == null && To == null) Period = "30";
                else if (From != null && To != null && From > To) (From, To) = (To, From);
            }
            else
            {
                From = null;
                To = null;
            }
            Q = string.IsNullOrWhiteSpace(Q) ? null : Q.Trim()[..Math.Min(Q.Trim().Length, 100)];
            Level = string.IsNullOrWhiteSpace(Level) ? null : Level.Trim().ToUpperInvariant();
            if (Level is not ("A1" or "A2" or "B1" or "B2" or "C1" or "C2")) Level = null;
            Status = Status is "pass" or "fail" ? Status : null;
            End = End is "submitted" or "timeout" or "violations" ? End : null;
            Sort = Sort is "date_asc" or "score" or "score_asc" or "student" or "quiz" or "duration" or "warnings" ? Sort : null;
            Size = PageSizes.Contains(Size) ? Size : 25;
            Page = Math.Max(1, Page);
            return this;
        }

        /// <summary>The chosen days as a UTC range [from, to); null ends are open.</summary>
        public (DateTime? From, DateTime? To) Range()
        {
            var today = ReportTime.Today;
            return Period switch
            {
                "all" => (null, null),
                "custom" => (From is { } f ? ReportTime.StartOfDayUtc(f) : null,
                             To is { } t ? ReportTime.StartOfDayUtc(t.AddDays(1)) : null),
                _ => (ReportTime.StartOfDayUtc(today.AddDays(1 - int.Parse(Period!, CultureInfo.InvariantCulture))),
                      ReportTime.StartOfDayUtc(today.AddDays(1)))
            };
        }

        /// <summary>The same number of days just before, for "compared with the previous period"; null when open-ended.</summary>
        public (DateTime From, DateTime To)? PreviousRange()
        {
            var (from, to) = Range();
            if (from is not { } f || to is not { } t) return null;
            return (f - (t - f), f);
        }

        public int Days()
        {
            var (from, to) = Range();
            return from is { } f && to is { } t ? Math.Max(1, (int)Math.Round((t - f).TotalDays)) : 0;
        }

        /// <summary>Everything that changes the data (not the page or sort), for cache keys.</summary>
        public string DataKey() => string.Join("|", Period, From, To, Quiz, Q?.ToLowerInvariant(), Level, Category, Status, End, First, Warn, ReportTime.Today);

        /// <summary>The filter as query-string values, for links (paging, sorting, removing one filter).</summary>
        public Dictionary<string, string?> ToQuery()
        {
            var query = new Dictionary<string, string?>();
            if (Period != "30") query["period"] = Period;
            if (From is { } f) query["from"] = f.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            if (To is { } t) query["to"] = t.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            if (Quiz is { } quiz) query["quiz"] = quiz.ToString(CultureInfo.InvariantCulture);
            if (Q != null) query["q"] = Q;
            if (Level != null) query["level"] = Level;
            if (Category is { } category) query["category"] = category.ToString(CultureInfo.InvariantCulture);
            if (Status != null) query["status"] = Status;
            if (End != null) query["end"] = End;
            if (First) query["first"] = "true";
            if (Warn) query["warn"] = "true";
            if (Sort != null) query["sort"] = Sort;
            if (Page > 1) query["page"] = Page.ToString(CultureInfo.InvariantCulture);
            if (Size != 25) query["size"] = Size.ToString(CultureInfo.InvariantCulture);
            return query;
        }

        public ExamReportFilter With(Action<ExamReportFilter> change)
        {
            var copy = (ExamReportFilter)MemberwiseClone();
            change(copy);
            return copy;
        }

        public bool HasFilters => Quiz != null || Q != null || Level != null || Category != null || Status != null || End != null || First || Warn;
    }
}
