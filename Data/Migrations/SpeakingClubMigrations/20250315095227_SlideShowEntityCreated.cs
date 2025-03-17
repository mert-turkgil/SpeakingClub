using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SpeakingClub.Data.Migrations.SpeakingClubMigrations
{
    /// <inheritdoc />
    public partial class SlideShowEntityCreated : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Comments_User_UserId",
                table: "Comments");

            migrationBuilder.DropForeignKey(
                name: "FK_Ratings_User_UserId",
                table: "Ratings");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "Ratings",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<int>(
                name: "CategoryId",
                table: "Quizzes",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "Comments",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AddColumn<bool>(
                name: "isHome",
                table: "Blogs",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "Slide",
                columns: table => new
                {
                    SlideId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CarouselTitle = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CarouselImage = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CarouselImage600w = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CarouselImage1200w = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CarouselDescription = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CarouselLink = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CarouselLinkText = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DateAdded = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Slide", x => x.SlideId);
                });

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

            migrationBuilder.InsertData(
                table: "Slide",
                columns: new[] { "SlideId", "CarouselDescription", "CarouselImage", "CarouselImage1200w", "CarouselImage600w", "CarouselLink", "CarouselLinkText", "CarouselTitle", "DateAdded" },
                values: new object[,]
                {
                    { 1, "Description for slide 1", "slide1.jpg", "slide1_1200w.jpg", "slide1_600w.jpg", "http://example.com/slide1", "Learn More", "Slide 1", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 2, "Description for slide 2", "slide2.jpg", "slide2_1200w.jpg", "slide2_600w.jpg", "http://example.com/slide2", "Learn More", "Slide 2", new DateTime(2025, 1, 15, 0, 0, 0, 0, DateTimeKind.Unspecified) }
                });

            migrationBuilder.AddForeignKey(
                name: "FK_Comments_User_UserId",
                table: "Comments",
                column: "UserId",
                principalTable: "User",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Ratings_User_UserId",
                table: "Ratings",
                column: "UserId",
                principalTable: "User",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Comments_User_UserId",
                table: "Comments");

            migrationBuilder.DropForeignKey(
                name: "FK_Ratings_User_UserId",
                table: "Ratings");

            migrationBuilder.DropTable(
                name: "Slide");

            migrationBuilder.DropColumn(
                name: "isHome",
                table: "Blogs");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "Ratings",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "CategoryId",
                table: "Quizzes",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "Comments",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Comments_User_UserId",
                table: "Comments",
                column: "UserId",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Ratings_User_UserId",
                table: "Ratings",
                column: "UserId",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
