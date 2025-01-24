using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace UI.Data.Migrations.ShopContextMigrations
{
    /// <inheritdoc />
    public partial class InitialCreateForShop : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Blogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Url = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    DateModified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Author = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ImageUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    VideoUrl = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Blogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Carousels",
                columns: table => new
                {
                    CarouselId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CarouselTitle = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CarouselImage = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    CarouselImage600w = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CarouselImage1200w = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CarouselDescription = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CarouselLink = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CarouselLinkText = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DateAdded = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Carousels", x => x.CarouselId);
                });

            migrationBuilder.CreateTable(
                name: "Quizzes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Soru = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Cevap = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Text = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Zaman = table.Column<int>(type: "int", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Quizzes", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "Blogs",
                columns: new[] { "Id", "Author", "Content", "DateCreated", "DateModified", "ImageUrl", "Title", "Url", "VideoUrl" },
                values: new object[,]
                {
                    { 1, "Frau Müller", "<p>In diesem Beitrag werden wir die Verwendung der deutschen Artikel <strong>der</strong>, <strong>die</strong> und <strong>das</strong> untersuchen. Diese kleinen Wörter sind entscheidend für das Verständnis und die korrekte Verwendung der deutschen Sprache.</p><p><strong>Der</strong> wird für maskuline Nomen verwendet, <strong>die</strong> für feminine und <strong>das</strong> für neutrale Nomen. Zum Beispiel:</p><ul><li>Der Mann</li><li>Die Frau</li><li>Das Kind</li></ul><p>Es ist wichtig, die Artikel mit den Nomen auswendig zu lernen, da sie keinen festen Regeln folgen.</p>", new DateTime(2025, 1, 24, 16, 24, 39, 401, DateTimeKind.Utc).AddTicks(5853), null, null, "Die Bedeutung der deutschen Artikel: Der, Die, Das", "sampleurl", null },
                    { 2, "Mehmet Hoca", "<p>Bu yazıda Türkçe'de fiillerin şimdiki zaman çekimini ele alacağız. Şimdiki zaman, şu anda gerçekleşen eylemleri ifade etmek için kullanılır.</p><p>Örnekler:</p><ul><li>Ben <strong>yazıyorum</strong>.</li><li>Sen <strong>okuyorsun</strong>.</li><li>O <strong>konuşuyor</strong>.</li></ul><p>Fiilin köküne uygun ekleri ekleyerek fiili çekimleyebiliriz.</p>", new DateTime(2025, 1, 24, 16, 24, 39, 401, DateTimeKind.Utc).AddTicks(5855), null, null, "Türkçe'de Fiillerin Çekimi: Şimdiki Zaman", "sampleurl2", null }
                });

            migrationBuilder.InsertData(
                table: "Carousels",
                columns: new[] { "CarouselId", "CarouselDescription", "CarouselImage", "CarouselImage1200w", "CarouselImage600w", "CarouselLink", "CarouselLinkText", "CarouselTitle" },
                values: new object[,]
                {
                    { 1, "Test", "cover1.jpg", "cover1.jpg", "cover1.jpg", "About", "Learn More", "Test" },
                    { 2, "Test2", "cover2.jpg", "cover2.jpg", "cover2.jpg", "About", "Learn More", "Test2" },
                    { 3, "Test", "cover3.jpg", "cover3.jpg", "cover3.jpg", "About", "Learn More", "Test3" }
                });

            migrationBuilder.InsertData(
                table: "Quizzes",
                columns: new[] { "Id", "Cevap", "Date", "Soru", "Text", "Zaman" },
                values: new object[,]
                {
                    { 1, "Text4", new DateTime(2025, 1, 24, 19, 24, 39, 401, DateTimeKind.Local).AddTicks(5676), "S1-S1P", "[\"Text1\",\"Text2\",\"Text3\",\"Text4\"]", 60 },
                    { 2, "Text1", new DateTime(2025, 1, 24, 19, 24, 39, 401, DateTimeKind.Local).AddTicks(5690), "S2-S2P", "[\"Text1\",\"Text2\",\"Text3\",\"Text4\"]", 60 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Blogs_DateCreated",
                table: "Blogs",
                column: "DateCreated");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Blogs");

            migrationBuilder.DropTable(
                name: "Carousels");

            migrationBuilder.DropTable(
                name: "Quizzes");
        }
    }
}
