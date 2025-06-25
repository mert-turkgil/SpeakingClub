using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpeakingClub.Data.Migrations.SpeakingClubMigrations
{
    /// <inheritdoc />
    public partial class FixingSeedingMistake : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Blogs",
                keyColumn: "BlogId",
                keyValue: 1,
                column: "Url",
                value: "blog1");

            migrationBuilder.UpdateData(
                table: "Blogs",
                keyColumn: "BlogId",
                keyValue: 2,
                column: "Url",
                value: "blog2");

            migrationBuilder.UpdateData(
                table: "QuizSubmissions",
                keyColumn: "QuizSubmissionId",
                keyValue: 1,
                column: "SubmissionDate",
                value: new DateTime(2025, 6, 23, 21, 59, 3, 577, DateTimeKind.Utc).AddTicks(6452));

            migrationBuilder.UpdateData(
                table: "QuizSubmissions",
                keyColumn: "QuizSubmissionId",
                keyValue: 2,
                column: "SubmissionDate",
                value: new DateTime(2025, 6, 24, 21, 59, 3, 577, DateTimeKind.Utc).AddTicks(6462));

            migrationBuilder.UpdateData(
                table: "QuizSubmissions",
                keyColumn: "QuizSubmissionId",
                keyValue: 3,
                column: "SubmissionDate",
                value: new DateTime(2025, 6, 24, 21, 59, 3, 577, DateTimeKind.Utc).AddTicks(6464));

            migrationBuilder.UpdateData(
                table: "User",
                keyColumn: "Id",
                keyValue: "teacher1",
                column: "ConcurrencyStamp",
                value: "b0794945-e66e-42bb-a302-92daffc0a8eb");

            migrationBuilder.UpdateData(
                table: "User",
                keyColumn: "Id",
                keyValue: "user1-id",
                columns: new[] { "ConcurrencyStamp", "SecurityStamp" },
                values: new object[] { "64a9730f-e890-4174-985c-8e80088affdb", "15f89987-057b-4e18-8b3c-cc6169d83296" });

            migrationBuilder.UpdateData(
                table: "User",
                keyColumn: "Id",
                keyValue: "user2-id",
                columns: new[] { "ConcurrencyStamp", "SecurityStamp" },
                values: new object[] { "7f463d19-b390-4fc4-ae01-a7b6c1befa6d", "d0d5c162-0c37-4520-9d49-e7b8e1a1df58" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Blogs",
                keyColumn: "BlogId",
                keyValue: 1,
                column: "Url",
                value: "http://example.com/blog1");

            migrationBuilder.UpdateData(
                table: "Blogs",
                keyColumn: "BlogId",
                keyValue: 2,
                column: "Url",
                value: "http://example.com/blog2");

            migrationBuilder.UpdateData(
                table: "QuizSubmissions",
                keyColumn: "QuizSubmissionId",
                keyValue: 1,
                column: "SubmissionDate",
                value: new DateTime(2025, 6, 22, 21, 51, 17, 616, DateTimeKind.Utc).AddTicks(591));

            migrationBuilder.UpdateData(
                table: "QuizSubmissions",
                keyColumn: "QuizSubmissionId",
                keyValue: 2,
                column: "SubmissionDate",
                value: new DateTime(2025, 6, 23, 21, 51, 17, 616, DateTimeKind.Utc).AddTicks(598));

            migrationBuilder.UpdateData(
                table: "QuizSubmissions",
                keyColumn: "QuizSubmissionId",
                keyValue: 3,
                column: "SubmissionDate",
                value: new DateTime(2025, 6, 23, 21, 51, 17, 616, DateTimeKind.Utc).AddTicks(609));

            migrationBuilder.UpdateData(
                table: "User",
                keyColumn: "Id",
                keyValue: "teacher1",
                column: "ConcurrencyStamp",
                value: "c07f7abd-8a55-4d6a-b5a0-e410d58627b4");

            migrationBuilder.UpdateData(
                table: "User",
                keyColumn: "Id",
                keyValue: "user1-id",
                columns: new[] { "ConcurrencyStamp", "SecurityStamp" },
                values: new object[] { "462b3d69-2d86-4a73-86f7-7d6c15286750", "3004496c-f638-455a-a5cc-0b41a436f1b9" });

            migrationBuilder.UpdateData(
                table: "User",
                keyColumn: "Id",
                keyValue: "user2-id",
                columns: new[] { "ConcurrencyStamp", "SecurityStamp" },
                values: new object[] { "cec18065-53b4-4fe9-ba10-7fa10126d1c8", "997f70b1-bd26-4b76-94ee-ed43942c0bc1" });
        }
    }
}
