using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpeakingClub.Data.Migrations.SpeakingClubMigrations
{
    /// <inheritdoc />
    public partial class ReportIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Identity ids are 36 characters; stop with a clear message rather than truncating anything
            migrationBuilder.Sql(@"IF EXISTS (SELECT 1 FROM QuizSubmissions WHERE LEN(UserId) > 450)
    THROW 50001, 'QuizSubmissions.UserId has values longer than 450 characters. Fix them before applying ReportIndexes.', 1;");

            migrationBuilder.DropIndex(
                name: "IX_QuizSubmissions_QuizId",
                table: "QuizSubmissions");

            migrationBuilder.DropIndex(
                name: "IX_QuizResponses_QuizSubmissionId",
                table: "QuizResponses");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "QuizSubmissions",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<bool>(
                name: "TimesRecorded",
                table: "QuizSubmissions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsCorrect",
                table: "QuizResponses",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TimesJson",
                table: "QuizAttempts",
                type: "nvarchar(max)",
                nullable: true);

            // Past results, made usable for the reports. Table names are unqualified: production tables may live in
            // the login's own schema.
            // 1. Correctness from the answer options as they are now. A skipped question ("No answer") counts as
            //    wrong; when the chosen option was deleted later (QuizAnswerId cleared) it stays NULL (unknown).
            // 2. The question of responses saved before QuestionId was stored.
            // 3. TimesRecorded where any time was measured (older attempts and early timeouts have none).
            migrationBuilder.Sql(@"UPDATE r SET IsCorrect = a.IsCorrect
FROM QuizResponses r INNER JOIN QuizAnswers a ON a.Id = r.QuizAnswerId
WHERE r.IsCorrect IS NULL;
UPDATE QuizResponses SET IsCorrect = 0
WHERE IsCorrect IS NULL AND QuizAnswerId IS NULL AND AnswerText = 'No answer';
UPDATE r SET QuestionId = a.QuestionId
FROM QuizResponses r INNER JOIN QuizAnswers a ON a.Id = r.QuizAnswerId
WHERE r.QuestionId IS NULL;
UPDATE s SET TimesRecorded = 1
FROM QuizSubmissions s
WHERE EXISTS (SELECT 1 FROM QuizResponses r WHERE r.QuizSubmissionId = s.QuizSubmissionId AND r.TimeTakenSeconds > 0);");

            migrationBuilder.CreateIndex(
                name: "IX_QuizSubmissions_QuizId_SubmissionDate",
                table: "QuizSubmissions",
                columns: new[] { "QuizId", "SubmissionDate" })
                .Annotation("SqlServer:Include", new[] { "UserId", "Score", "AttemptNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_QuizSubmissions_SubmissionDate",
                table: "QuizSubmissions",
                column: "SubmissionDate")
                .Annotation("SqlServer:Include", new[] { "QuizId", "UserId", "Score", "DurationSeconds", "ViolationCount", "EndReason", "AttemptNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_QuizSubmissions_UserId_QuizId_SubmissionDate",
                table: "QuizSubmissions",
                columns: new[] { "UserId", "QuizId", "SubmissionDate" })
                .Annotation("SqlServer:Include", new[] { "Score" });

            migrationBuilder.CreateIndex(
                name: "IX_QuizResponses_QuestionId",
                table: "QuizResponses",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_QuizResponses_QuizSubmissionId",
                table: "QuizResponses",
                column: "QuizSubmissionId")
                .Annotation("SqlServer:Include", new[] { "QuestionId", "QuizAnswerId", "IsCorrect", "TimeTakenSeconds" });

            migrationBuilder.CreateIndex(
                name: "IX_QuizAttempts_SubmittedAt_ExpiresAt",
                table: "QuizAttempts",
                columns: new[] { "SubmittedAt", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_DownloadLogs_UserId_DownloadedAt",
                table: "DownloadLogs",
                columns: new[] { "UserId", "DownloadedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_QuizSubmissions_QuizId_SubmissionDate",
                table: "QuizSubmissions");

            migrationBuilder.DropIndex(
                name: "IX_QuizSubmissions_SubmissionDate",
                table: "QuizSubmissions");

            migrationBuilder.DropIndex(
                name: "IX_QuizSubmissions_UserId_QuizId_SubmissionDate",
                table: "QuizSubmissions");

            migrationBuilder.DropIndex(
                name: "IX_QuizResponses_QuestionId",
                table: "QuizResponses");

            migrationBuilder.DropIndex(
                name: "IX_QuizResponses_QuizSubmissionId",
                table: "QuizResponses");

            migrationBuilder.DropIndex(
                name: "IX_QuizAttempts_SubmittedAt_ExpiresAt",
                table: "QuizAttempts");

            migrationBuilder.DropIndex(
                name: "IX_DownloadLogs_UserId_DownloadedAt",
                table: "DownloadLogs");

            migrationBuilder.DropColumn(
                name: "TimesRecorded",
                table: "QuizSubmissions");

            migrationBuilder.DropColumn(
                name: "IsCorrect",
                table: "QuizResponses");

            migrationBuilder.DropColumn(
                name: "TimesJson",
                table: "QuizAttempts");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "QuizSubmissions",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldMaxLength: 450);

            migrationBuilder.CreateIndex(
                name: "IX_QuizSubmissions_QuizId",
                table: "QuizSubmissions",
                column: "QuizId");

            migrationBuilder.CreateIndex(
                name: "IX_QuizResponses_QuizSubmissionId",
                table: "QuizResponses",
                column: "QuizSubmissionId");
        }
    }
}
