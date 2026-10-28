using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpeakingClub.Data.Migrations.IdentityMigrations
{
    /// <inheritdoc />
    public partial class MemberFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EmailOptOuts",
                table: "AspNetUsers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "EmailPreferencesToken",
                table: "AspNetUsers",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastLoginAt",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PreferredCulture",
                table: "AspNetUsers",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PromoConsentAt",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PromoConsentSource",
                table: "AspNetUsers",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            // Existing accounts: subscribed to announcements as "legacy" (the site owner's decision; they can opt out
            // in one click, and EmailThrottle:IncludeLegacyOptIns=false leaves them out without a deploy).
            // Their registration date is estimated from their first activity (exam started or submitted, file
            // downloaded); without activity it stays NULL ("older account"). The exam tables belong to the other
            // context and may not exist yet on a brand-new database, hence the check and the dynamic SQL.
            // Table names are unqualified: production tables may live in the login's own schema.
            migrationBuilder.Sql("UPDATE AspNetUsers SET PromoConsentSource = 'legacy' WHERE PromoConsentSource IS NULL;");
            migrationBuilder.Sql(@"IF OBJECT_ID(N'QuizAttempts') IS NOT NULL AND OBJECT_ID(N'QuizSubmissions') IS NOT NULL AND OBJECT_ID(N'DownloadLogs') IS NOT NULL
EXEC(N'UPDATE u SET CreatedAt = x.FirstSeen
FROM AspNetUsers u
CROSS APPLY (SELECT MIN(t.d) AS FirstSeen FROM (
    SELECT MIN(StartedAt) AS d FROM QuizAttempts WHERE UserId = u.Id
    UNION ALL SELECT MIN(SubmissionDate) FROM QuizSubmissions WHERE UserId = u.Id
    UNION ALL SELECT MIN(DownloadedAt) FROM DownloadLogs WHERE UserId = u.Id) t) x
WHERE u.CreatedAt IS NULL AND x.FirstSeen IS NOT NULL');");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_CreatedAt",
                table: "AspNetUsers",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_EmailPreferencesToken",
                table: "AspNetUsers",
                column: "EmailPreferencesToken",
                unique: true,
                filter: "[EmailPreferencesToken] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_CreatedAt",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_EmailPreferencesToken",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "EmailOptOuts",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "EmailPreferencesToken",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "LastLoginAt",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "PreferredCulture",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "PromoConsentAt",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "PromoConsentSource",
                table: "AspNetUsers");
        }
    }
}
