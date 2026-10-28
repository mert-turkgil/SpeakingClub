using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SpeakingClub.Entity;
using SpeakingClub.Models;

namespace SpeakingClub.Identity
{
    public class ApplicationDbContext : IdentityDbContext<User>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            builder.Entity<User>(user =>
            {
                // Bounded so the reports can sort by name cheaply (sorting nvarchar(max) makes SQL Server reserve a lot of memory)
                user.Property(u => u.FirstName).HasMaxLength(User.NameMaxLength);
                user.Property(u => u.LastName).HasMaxLength(User.NameMaxLength);
                user.Property(u => u.PreferredCulture).HasMaxLength(10);
                user.Property(u => u.PromoConsentSource).HasMaxLength(20);
                user.Property(u => u.EmailPreferencesToken).HasMaxLength(64);
                user.HasIndex(u => u.CreatedAt);
                user.HasIndex(u => u.EmailPreferencesToken).IsUnique().HasFilter("[EmailPreferencesToken] IS NOT NULL");
            });

            // Exam activity per member for the member report (Entity/ReportProjections.cs): read-only, not in migrations
            builder.Entity<MemberActivity>()
                .HasNoKey()
                .ToTable((string?)null)   // read through the query below only: no table, no migration
                .ToSqlQuery(@"SELECT UserId, COUNT(*) AS Attempts, COUNT(DISTINCT QuizId) AS Quizzes,
       AVG(CAST(Score AS float)) AS AverageScore, MAX(Score) AS BestScore,
       MIN(SubmissionDate) AS FirstExamAt, MAX(SubmissionDate) AS LastExamAt
FROM QuizSubmissions GROUP BY UserId");
        }

        // Read-only (Entity/ReportProjections.cs)
        public DbSet<MemberActivity> MemberActivities { get; set; }
    }
}