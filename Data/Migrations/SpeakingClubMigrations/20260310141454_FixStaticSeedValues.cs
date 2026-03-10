using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpeakingClub.Data.Migrations.SpeakingClubMigrations
{
    /// <inheritdoc />
    public partial class FixStaticSeedValues : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "QuizSubmissions",
                keyColumn: "QuizSubmissionId",
                keyValue: 1,
                column: "SubmissionDate",
                value: new DateTime(2026, 3, 8, 12, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.UpdateData(
                table: "QuizSubmissions",
                keyColumn: "QuizSubmissionId",
                keyValue: 2,
                column: "SubmissionDate",
                value: new DateTime(2026, 3, 9, 12, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.UpdateData(
                table: "QuizSubmissions",
                keyColumn: "QuizSubmissionId",
                keyValue: 3,
                column: "SubmissionDate",
                value: new DateTime(2026, 3, 9, 12, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.UpdateData(
                table: "User",
                keyColumn: "Id",
                keyValue: "teacher1",
                column: "ConcurrencyStamp",
                value: "6db5f62e-5d87-4cf5-b857-00354ac651e5");

            migrationBuilder.UpdateData(
                table: "User",
                keyColumn: "Id",
                keyValue: "user1-id",
                columns: new[] { "ConcurrencyStamp", "SecurityStamp" },
                values: new object[] { "dd1170ec-c590-4f87-9b0b-05c127c69964", "b8e7c3d4-5a6f-4e2d-9c1b-8a7f6e5d4c3b" });

            migrationBuilder.UpdateData(
                table: "User",
                keyColumn: "Id",
                keyValue: "user2-id",
                columns: new[] { "ConcurrencyStamp", "SecurityStamp" },
                values: new object[] { "577968cc-e0fd-49fa-9011-003c770b3260", "c9f8d4e5-6b7g-5f3e-ad2c-9b8g7f6e5d4c" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
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
    }
}
