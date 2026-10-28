using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace SpeakingClub.Services.Reports
{
    /// <summary>
    /// The filters of the member report (Admin → Kullanıcılar), read from the address bar like the exam report's
    /// (ExamReportFilter). The period is the sign-up date; by default every member is listed, newest first.
    /// </summary>
    public class MemberReportFilter
    {
        public static readonly string[] Periods = { "7", "30", "90", "365", "all", "custom" };
        public static readonly int[] PageSizes = { 25, 50, 100 };
        public const int ActiveDays = 30;     // "active": an exam in the last 30 days
        public const int DormantDays = 60;    // "dormant": took exams, none in the last 60 days

        public string? Period { get; set; }
        public DateOnly? From { get; set; }
        public DateOnly? To { get; set; }
        public string? Q { get; set; }          // part of the name, e-mail address or user name
        public string? Role { get; set; }       // student | teacher | admin
        public string? Activity { get; set; }   // exams | none | active | dormant
        public string? Account { get; set; }    // unconfirmed | locked | google
        public string? Promo { get; set; }      // in | out (announcement e-mails)
        public string? Sort { get; set; }
        public int Page { get; set; } = 1;
        public int Size { get; set; } = 25;

        public MemberReportFilter Normalize()
        {
            Period = Periods.Contains(Period) ? Period : "all";
            if (Period == "custom")
            {
                if (From == null && To == null) Period = "all";
                else if (From != null && To != null && From > To) (From, To) = (To, From);
            }
            else
            {
                From = null;
                To = null;
            }
            Q = string.IsNullOrWhiteSpace(Q) ? null : Q.Trim()[..Math.Min(Q.Trim().Length, 100)];
            Role = Role is "student" or "teacher" or "admin" ? Role : null;
            Activity = Activity is "exams" or "none" or "active" or "dormant" ? Activity : null;
            Account = Account is "unconfirmed" or "locked" or "google" ? Account : null;
            Promo = Promo is "in" or "out" ? Promo : null;
            Sort = Sort is "oldest" or "name" or "activity" or "exams" or "score" ? Sort : null;
            Size = PageSizes.Contains(Size) ? Size : 25;
            Page = Math.Max(1, Page);
            return this;
        }

        /// <summary>Sign-up dates as a UTC range [from, to); null ends are open.</summary>
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

        /// <summary>Everything that changes the data (not the page or sort), for cache keys.</summary>
        public string DataKey() => string.Join("|", Period, From, To, Q?.ToLowerInvariant(), Role, Activity, Account, Promo, ReportTime.Today);

        public Dictionary<string, string?> ToQuery()
        {
            var query = new Dictionary<string, string?>();
            if (Period != "all") query["period"] = Period;
            if (From is { } f) query["from"] = f.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            if (To is { } t) query["to"] = t.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            if (Q != null) query["q"] = Q;
            if (Role != null) query["role"] = Role;
            if (Activity != null) query["activity"] = Activity;
            if (Account != null) query["account"] = Account;
            if (Promo != null) query["promo"] = Promo;
            if (Sort != null) query["sort"] = Sort;
            if (Page > 1) query["page"] = Page.ToString(CultureInfo.InvariantCulture);
            if (Size != 25) query["size"] = Size.ToString(CultureInfo.InvariantCulture);
            return query;
        }

        public MemberReportFilter With(Action<MemberReportFilter> change)
        {
            var copy = (MemberReportFilter)MemberwiseClone();
            change(copy);
            return copy;
        }

        public bool HasFilters => Q != null || Role != null || Activity != null || Account != null || Promo != null;
    }
}
