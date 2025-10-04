using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpeakingClub.Data.Migrations.SpeakingClubMigrations
{
    /// <inheritdoc />
    public partial class RenameTeacherIdToTeacherName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Quizzes_User_TeacherId",
                table: "Quizzes");

            migrationBuilder.DropIndex(
                name: "IX_Quizzes_TeacherId",
                table: "Quizzes");

            migrationBuilder.DropColumn(
                name: "TeacherId",
                table: "Quizzes");

            migrationBuilder.AddColumn<string>(
                name: "TeacherName",
                table: "Quizzes",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "QuizSubmissions",
                keyColumn: "QuizSubmissionId",
                keyValue: 1,
                column: "SubmissionDate",
                value: new DateTime(2025, 10, 2, 17, 58, 43, 889, DateTimeKind.Utc).AddTicks(7605));

            migrationBuilder.UpdateData(
                table: "QuizSubmissions",
                keyColumn: "QuizSubmissionId",
                keyValue: 2,
                column: "SubmissionDate",
                value: new DateTime(2025, 10, 3, 17, 58, 43, 889, DateTimeKind.Utc).AddTicks(7615));

            migrationBuilder.UpdateData(
                table: "QuizSubmissions",
                keyColumn: "QuizSubmissionId",
                keyValue: 3,
                column: "SubmissionDate",
                value: new DateTime(2025, 10, 3, 17, 58, 43, 889, DateTimeKind.Utc).AddTicks(7617));

            migrationBuilder.UpdateData(
                table: "Quizzes",
                keyColumn: "Id",
                keyValue: 1,
                column: "TeacherName",
                value: "teacher1");

            migrationBuilder.UpdateData(
                table: "User",
                keyColumn: "Id",
                keyValue: "teacher1",
                column: "ConcurrencyStamp",
                value: "ce4349db-3704-4800-8d2e-011271ade94f");

            migrationBuilder.UpdateData(
                table: "User",
                keyColumn: "Id",
                keyValue: "user1-id",
                columns: new[] { "ConcurrencyStamp", "SecurityStamp" },
                values: new object[] { "b12647bb-6772-41a1-a756-608375d88965", "5b089e9e-01f9-40e8-910a-4e35c87ed1c6" });

            migrationBuilder.UpdateData(
                table: "User",
                keyColumn: "Id",
                keyValue: "user2-id",
                columns: new[] { "ConcurrencyStamp", "SecurityStamp" },
                values: new object[] { "ac21837b-dc17-4f3b-9ffb-2713e0c51731", "958765e6-0e11-45e1-b099-ca54f51a2fab" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TeacherName",
                table: "Quizzes");

            migrationBuilder.AddColumn<string>(
                name: "TeacherId",
                table: "Quizzes",
                type: "nvarchar(450)",
                nullable: true);

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
                table: "Quizzes",
                keyColumn: "Id",
                keyValue: 1,
                column: "TeacherId",
                value: "teacher1");

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

            migrationBuilder.CreateIndex(
                name: "IX_Quizzes_TeacherId",
                table: "Quizzes",
                column: "TeacherId");

            migrationBuilder.AddForeignKey(
                name: "FK_Quizzes_User_TeacherId",
                table: "Quizzes",
                column: "TeacherId",
                principalTable: "User",
                principalColumn: "Id");
        }
    }
}
