using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpeakingClub.Data.Migrations.SpeakingClubMigrations
{
    /// <inheritdoc />
    public partial class WordsDetailed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Origin",
                table: "Words",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Pronunciation",
                table: "Words",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Synonyms",
                table: "Words",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Blogs",
                keyColumn: "BlogId",
                keyValue: 1,
                column: "isHome",
                value: true);

            migrationBuilder.UpdateData(
                table: "Blogs",
                keyColumn: "BlogId",
                keyValue: 2,
                column: "isHome",
                value: true);

            migrationBuilder.UpdateData(
                table: "Words",
                keyColumn: "WordId",
                keyValue: 1,
                columns: new[] { "Origin", "Pronunciation", "Synonyms" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "Words",
                keyColumn: "WordId",
                keyValue: 2,
                columns: new[] { "Origin", "Pronunciation", "Synonyms" },
                values: new object[] { null, null, null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Origin",
                table: "Words");

            migrationBuilder.DropColumn(
                name: "Pronunciation",
                table: "Words");

            migrationBuilder.DropColumn(
                name: "Synonyms",
                table: "Words");

            migrationBuilder.UpdateData(
                table: "Blogs",
                keyColumn: "BlogId",
                keyValue: 1,
                column: "isHome",
                value: false);

            migrationBuilder.UpdateData(
                table: "Blogs",
                keyColumn: "BlogId",
                keyValue: 2,
                column: "isHome",
                value: false);
        }
    }
}
