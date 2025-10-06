using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpeakingClub.Data.Migrations.SpeakingClubMigrations
{
    /// <inheritdoc />
    public partial class AddBlogDescription : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Blogs",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Blogs",
                keyColumn: "BlogId",
                keyValue: 1,
                column: "Description",
                value: "");

            migrationBuilder.UpdateData(
                table: "Blogs",
                keyColumn: "BlogId",
                keyValue: 2,
                column: "Description",
                value: "");

            migrationBuilder.UpdateData(
                table: "QuizSubmissions",
                keyColumn: "QuizSubmissionId",
                keyValue: 1,
                column: "SubmissionDate",
                value: new DateTime(2025, 10, 4, 0, 17, 22, 185, DateTimeKind.Utc).AddTicks(3859));

            migrationBuilder.UpdateData(
                table: "QuizSubmissions",
                keyColumn: "QuizSubmissionId",
                keyValue: 2,
                column: "SubmissionDate",
                value: new DateTime(2025, 10, 5, 0, 17, 22, 185, DateTimeKind.Utc).AddTicks(3870));

            migrationBuilder.UpdateData(
                table: "QuizSubmissions",
                keyColumn: "QuizSubmissionId",
                keyValue: 3,
                column: "SubmissionDate",
                value: new DateTime(2025, 10, 5, 0, 17, 22, 185, DateTimeKind.Utc).AddTicks(3872));

            migrationBuilder.UpdateData(
                table: "User",
                keyColumn: "Id",
                keyValue: "teacher1",
                column: "ConcurrencyStamp",
                value: "d49bfa2d-6953-477e-9204-1a45ae00a550");

            migrationBuilder.UpdateData(
                table: "User",
                keyColumn: "Id",
                keyValue: "user1-id",
                columns: new[] { "ConcurrencyStamp", "SecurityStamp" },
                values: new object[] { "49c15bd2-9e91-4e1f-a853-06d3fa00ca0f", "5cc780b2-681d-48f1-848d-d8b320f7e87a" });

            migrationBuilder.UpdateData(
                table: "User",
                keyColumn: "Id",
                keyValue: "user2-id",
                columns: new[] { "ConcurrencyStamp", "SecurityStamp" },
                values: new object[] { "587310bf-8901-4f09-ae85-cb0d20b7cb2e", "11374ef4-6c78-439d-a03e-0622e40f5119" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Description",
                table: "Blogs");

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
    }
}
