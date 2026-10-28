using System;

namespace SpeakingClub.Entity
{
    // Read-only views of the other context's table, for the admin reports (Services/Reports). Both contexts use one
    // database, so each can read the other's table through ToSqlQuery: nothing is created in the database and the
    // types never appear in migrations. Never saved. Table names stay unqualified: production tables may live in
    // the login's own schema, exactly like the ones EF itself queries.

    /// <summary>A member's name and e-mail (AspNetUsers), for searching and sorting exam results by student.</summary>
    public class ReportUser
    {
        public string UserId { get; set; } = string.Empty;
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
    }

    /// <summary>A member's exam activity (QuizSubmissions grouped by user), for the member report.</summary>
    public class MemberActivity
    {
        public string UserId { get; set; } = string.Empty;
        public int Attempts { get; set; }
        public int Quizzes { get; set; }
        public double? AverageScore { get; set; }
        public int? BestScore { get; set; }
        public DateTime? FirstExamAt { get; set; }
        public DateTime? LastExamAt { get; set; }
    }
}
