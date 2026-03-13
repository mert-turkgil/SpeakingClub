using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpeakingClub.Data.Migrations.SpeakingClubMigrations
{
    /// <inheritdoc />
    public partial class AddDownloadLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DownloadLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    UserEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    UserFullName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    BlogFileId = table.Column<int>(type: "int", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    DownloadedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DownloadLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DownloadLogs_BlogFiles_BlogFileId",
                        column: x => x.BlogFileId,
                        principalTable: "BlogFiles",
                        principalColumn: "FileId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "User",
                keyColumn: "Id",
                keyValue: "teacher1",
                column: "ConcurrencyStamp",
                value: "5a824f29-c254-49ca-ba1f-12ecc01551a4");

            migrationBuilder.UpdateData(
                table: "User",
                keyColumn: "Id",
                keyValue: "user1-id",
                column: "ConcurrencyStamp",
                value: "d2cf90ef-2a83-4ae7-94b8-9f5ff360ff86");

            migrationBuilder.UpdateData(
                table: "User",
                keyColumn: "Id",
                keyValue: "user2-id",
                column: "ConcurrencyStamp",
                value: "60ea8f01-16f6-4c5b-8ca3-a7d2b8d25c8b");

            migrationBuilder.CreateIndex(
                name: "IX_DownloadLogs_BlogFileId",
                table: "DownloadLogs",
                column: "BlogFileId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DownloadLogs");

            migrationBuilder.UpdateData(
                table: "User",
                keyColumn: "Id",
                keyValue: "teacher1",
                column: "ConcurrencyStamp",
                value: "1926fc99-73a0-495c-ab75-9ba7d96c5c3f");

            migrationBuilder.UpdateData(
                table: "User",
                keyColumn: "Id",
                keyValue: "user1-id",
                column: "ConcurrencyStamp",
                value: "a6860dfc-fbec-4553-943e-261b4e79b78c");

            migrationBuilder.UpdateData(
                table: "User",
                keyColumn: "Id",
                keyValue: "user2-id",
                column: "ConcurrencyStamp",
                value: "7292971c-692d-4b49-b515-5bf1c43d7746");
        }
    }
}
