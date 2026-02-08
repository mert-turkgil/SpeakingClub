using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpeakingClub.Data.Migrations.SpeakingClubMigrations
{
    /// <inheritdoc />
    public partial class AddQuizSeoFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DescriptionDe",
                table: "Quizzes",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MetaDescription",
                table: "Quizzes",
                type: "nvarchar(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MetaDescriptionDe",
                table: "Quizzes",
                type: "nvarchar(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MetaKeywords",
                table: "Quizzes",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MetaKeywordsDe",
                table: "Quizzes",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Slug",
                table: "Quizzes",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SlugDe",
                table: "Quizzes",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TitleDe",
                table: "Quizzes",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.UpdateData(
                table: "QuizSubmissions",
                keyColumn: "QuizSubmissionId",
                keyValue: 1,
                column: "SubmissionDate",
                value: new DateTime(2026, 2, 6, 10, 39, 19, 325, DateTimeKind.Utc).AddTicks(4919));

            migrationBuilder.UpdateData(
                table: "QuizSubmissions",
                keyColumn: "QuizSubmissionId",
                keyValue: 2,
                column: "SubmissionDate",
                value: new DateTime(2026, 2, 7, 10, 39, 19, 325, DateTimeKind.Utc).AddTicks(4928));

            migrationBuilder.UpdateData(
                table: "QuizSubmissions",
                keyColumn: "QuizSubmissionId",
                keyValue: 3,
                column: "SubmissionDate",
                value: new DateTime(2026, 2, 7, 10, 39, 19, 325, DateTimeKind.Utc).AddTicks(4930));

            migrationBuilder.UpdateData(
                table: "Quizzes",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "DescriptionDe", "MetaDescription", "MetaDescriptionDe", "MetaKeywords", "MetaKeywordsDe", "Slug", "SlugDe", "TitleDe" },
                values: new object[] { "", "", "", "", "", "", "", "" });

            migrationBuilder.UpdateData(
                table: "User",
                keyColumn: "Id",
                keyValue: "teacher1",
                column: "ConcurrencyStamp",
                value: "1557cd84-5ef3-4236-8525-f305c715569d");

            migrationBuilder.UpdateData(
                table: "User",
                keyColumn: "Id",
                keyValue: "user1-id",
                columns: new[] { "ConcurrencyStamp", "SecurityStamp" },
                values: new object[] { "79d33df0-670d-41cd-94bc-e9a55da5e076", "4a9b56d8-e846-4e16-a2c3-2d0916d74bb7" });

            migrationBuilder.UpdateData(
                table: "User",
                keyColumn: "Id",
                keyValue: "user2-id",
                columns: new[] { "ConcurrencyStamp", "SecurityStamp" },
                values: new object[] { "a363bda0-6881-4340-a92b-de1fad6f2f35", "459834b4-a344-4e96-9c28-f64d29b1d6af" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DescriptionDe",
                table: "Quizzes");

            migrationBuilder.DropColumn(
                name: "MetaDescription",
                table: "Quizzes");

            migrationBuilder.DropColumn(
                name: "MetaDescriptionDe",
                table: "Quizzes");

            migrationBuilder.DropColumn(
                name: "MetaKeywords",
                table: "Quizzes");

            migrationBuilder.DropColumn(
                name: "MetaKeywordsDe",
                table: "Quizzes");

            migrationBuilder.DropColumn(
                name: "Slug",
                table: "Quizzes");

            migrationBuilder.DropColumn(
                name: "SlugDe",
                table: "Quizzes");

            migrationBuilder.DropColumn(
                name: "TitleDe",
                table: "Quizzes");

            migrationBuilder.UpdateData(
                table: "QuizSubmissions",
                keyColumn: "QuizSubmissionId",
                keyValue: 1,
                column: "SubmissionDate",
                value: new DateTime(2026, 1, 26, 15, 16, 43, 858, DateTimeKind.Utc).AddTicks(2756));

            migrationBuilder.UpdateData(
                table: "QuizSubmissions",
                keyColumn: "QuizSubmissionId",
                keyValue: 2,
                column: "SubmissionDate",
                value: new DateTime(2026, 1, 27, 15, 16, 43, 858, DateTimeKind.Utc).AddTicks(2762));

            migrationBuilder.UpdateData(
                table: "QuizSubmissions",
                keyColumn: "QuizSubmissionId",
                keyValue: 3,
                column: "SubmissionDate",
                value: new DateTime(2026, 1, 27, 15, 16, 43, 858, DateTimeKind.Utc).AddTicks(2764));

            migrationBuilder.UpdateData(
                table: "User",
                keyColumn: "Id",
                keyValue: "teacher1",
                column: "ConcurrencyStamp",
                value: "602f3161-b042-4b87-b3c8-b167ca6a3323");

            migrationBuilder.UpdateData(
                table: "User",
                keyColumn: "Id",
                keyValue: "user1-id",
                columns: new[] { "ConcurrencyStamp", "SecurityStamp" },
                values: new object[] { "ca360115-f68b-4c7c-a0f4-a9f2c7eaee22", "9a8cb8b9-af54-41b5-ad05-ff74bdab2da1" });

            migrationBuilder.UpdateData(
                table: "User",
                keyColumn: "Id",
                keyValue: "user2-id",
                columns: new[] { "ConcurrencyStamp", "SecurityStamp" },
                values: new object[] { "df4781c3-eede-48d8-a77b-8da71e494167", "84b1a7ed-94c2-4799-98f7-71765f6b19da" });
        }
    }
}
