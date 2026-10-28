using System;

namespace SpeakingClub.Services.Reports
{
    /// <summary>
    /// Dates in the reports: stored in UTC, shown and filtered in Turkish time (the teacher's clock; Turkey has
    /// no daylight saving, so a day is always UTC+3). "Last 7 days" means the last 7 calendar days there.
    /// </summary>
    public static class ReportTime
    {
        public static readonly TimeZoneInfo Zone = Find();

        private static TimeZoneInfo Find()
        {
            foreach (var id in new[] { "Europe/Istanbul", "Turkey Standard Time" })
                if (TimeZoneInfo.TryFindSystemTimeZoneById(id, out var zone))
                    return zone;
            return TimeZoneInfo.CreateCustomTimeZone("TRT", TimeSpan.FromHours(3), "Türkiye", "Türkiye");
        }

        public static DateTime ToLocal(DateTime utc) =>
            TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Unspecified), Zone);

        public static DateTime ToUtc(DateTime local) =>
            TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(local, DateTimeKind.Unspecified), Zone);

        /// <summary>Today's date in Turkey.</summary>
        public static DateOnly Today => DateOnly.FromDateTime(ToLocal(DateTime.UtcNow));

        /// <summary>The UTC instant a Turkish calendar day starts.</summary>
        public static DateTime StartOfDayUtc(DateOnly day) => ToUtc(day.ToDateTime(TimeOnly.MinValue));
    }
}
