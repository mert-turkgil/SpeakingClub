using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpeakingClub.Data.Migrations.SpeakingClubMigrations
{
    /// <inheritdoc />
    public partial class AddMultilingualAndSEOSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CanonicalUrl",
                table: "Blogs",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FocusKeyphrase",
                table: "Blogs",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsPublished",
                table: "Blogs",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastModified",
                table: "Blogs",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MetaDescription",
                table: "Blogs",
                type: "nvarchar(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MetaKeywords",
                table: "Blogs",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MetaTitle",
                table: "Blogs",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "NoFollow",
                table: "Blogs",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "NoIndex",
                table: "Blogs",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "OgDescription",
                table: "Blogs",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OgImage",
                table: "Blogs",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OgTitle",
                table: "Blogs",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Slug",
                table: "Blogs",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ViewCount",
                table: "Blogs",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "BlogTranslations",
                columns: table => new
                {
                    TranslationId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BlogId = table.Column<int>(type: "int", nullable: false),
                    LanguageCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Slug = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    MetaDescription = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                    MetaKeywords = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    MetaTitle = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    OgTitle = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    OgDescription = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BlogTranslations", x => x.TranslationId);
                    table.ForeignKey(
                        name: "FK_BlogTranslations_Blogs_BlogId",
                        column: x => x.BlogId,
                        principalTable: "Blogs",
                        principalColumn: "BlogId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "Blogs",
                keyColumn: "BlogId",
                keyValue: 1,
                columns: new[] { "Author", "CanonicalUrl", "Content", "Date", "FocusKeyphrase", "Image", "IsPublished", "LastModified", "MetaDescription", "MetaKeywords", "MetaTitle", "NoFollow", "NoIndex", "OgDescription", "OgImage", "OgTitle", "Slug", "Title", "Url", "ViewCount" },
                values: new object[] { "Suna Türkgil", "", "<h2>Almanca Konuşma Becerilerinizi Geliştirin</h2>\r\n<p>Almanca öğrenirken en zorlu kısımlardan biri konuşma pratiği yapmaktır. Bu yazıda, Almanca konuşma becerilerinizi geliştirebileceğiniz 5 etkili yöntemi paylaşıyorum.</p>\r\n\r\n<h3>1. Günlük Konuşma Pratiği</h3>\r\n<p>Her gün en az 15-20 dakika Almanca konuşma pratiği yapın. Kendinizle konuşabilir, günlük olayları Almanca anlatabilirsiniz.</p>\r\n\r\n<h3>2. Dizi ve Film İzleyin</h3>\r\n<p>Almanca altyazılı Alman dizileri ve filmleri izleyerek hem dinleme hem de konuşma becerilerinizi geliştirebilirsiniz.</p>\r\n\r\n<h3>3. Tandem Partneri Bulun</h3>\r\n<p>Almanca öğrenen veya anadili Almanca olan biriyle tandem yaparak karşılıklı pratik yapabilirsiniz.</p>\r\n\r\n<h3>4. Konuşma Kulüplerine Katılın</h3>\r\n<p>Online veya yüz yüze konuşma kulüplerine katılarak gerçek ortamda pratik yapma şansı yakalarsınız.</p>\r\n\r\n<h3>5. Sesli Kitap Dinleyin ve Tekrar Edin</h3>\r\n<p>Almanca sesli kitapları dinleyerek telaffuzunuzu geliştirebilir, duyduklarınızı tekrar ederek pratik yapabilirsiniz.</p>\r\n\r\n<p><strong>Unutmayın:</strong> Düzenli pratik, başarının anahtarıdır!</p>", new DateTime(2025, 12, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), "", "blog-speaking-practice.jpg", true, null, "", "", "", false, false, "", "", "", "", "Almanca Konuşma Pratiği İçin 5 Etkili Yöntem", "almanca-konusma-pratigi-5-yontem", 0 });

            migrationBuilder.UpdateData(
                table: "Blogs",
                keyColumn: "BlogId",
                keyValue: 2,
                columns: new[] { "Author", "CanonicalUrl", "CategoryId", "Content", "Date", "FocusKeyphrase", "Image", "IsPublished", "LastModified", "MetaDescription", "MetaKeywords", "MetaTitle", "NoFollow", "NoIndex", "OgDescription", "OgImage", "OgTitle", "Slug", "Title", "Url", "ViewCount" },
                values: new object[] { "Suna Türkgil", "", 1, "<h2>Almanca Öğrenirken Dikkat Edilmesi Gereken Noktalar</h2>\r\n<p>Almanca öğrenirken herkesin yaptığı bazı yaygın hatalar vardır. Bu yazıda en sık karşılaşılan 10 grameri hatasını ve bunlardan nasıl kaçınılacağını ele alıyoruz.</p>\r\n\r\n<h3>1. Artikel Hataları</h3>\r\n<p>Der, die, das artikellerini karıştırmak en yaygın hatadır. Her kelimeyi artikeliyle birlikte ezberleyin.</p>\r\n\r\n<h3>2. Fiil Çekimleri</h3>\r\n<p>Almancada fiiller özneye göre çekimlenir. \"ich gehe\", \"du gehst\", \"er/sie/es geht\" şeklinde değişir.</p>\r\n\r\n<h3>3. Sözcük Dizilimi</h3>\r\n<p>Almancada fiil genellikle ikinci sırada gelir. Yan cümlelerde ise fiil en sona gider.</p>\r\n\r\n<h3>4. Dativ ve Akkusativ</h3>\r\n<p>Hangi fiilin hangi hali istediğini öğrenmek çok önemlidir. \"helfen\" dativ isterken, \"sehen\" akkusativ ister.</p>\r\n\r\n<h3>5. Präpositionen (Edatlar)</h3>\r\n<p>Her edatın belirli bir hali gerektirdiğini unutmayın. \"mit\" her zaman dativ, \"für\" her zaman akkusativ ister.</p>\r\n\r\n<p><strong>İpucu:</strong> Hata yapmaktan korkmayın! Hatalar öğrenmenin doğal bir parçasıdır.</p>", new DateTime(2026, 1, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), "", "blog-grammar-mistakes.jpg", true, null, "", "", "", false, false, "", "", "", "", "Almanca Gramer: En Sık Yapılan 10 Hata", "almanca-gramer-10-yaygin-hata", 0 });

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

            migrationBuilder.CreateIndex(
                name: "IX_BlogTranslations_BlogId_LanguageCode",
                table: "BlogTranslations",
                columns: new[] { "BlogId", "LanguageCode" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BlogTranslations");

            migrationBuilder.DropColumn(
                name: "CanonicalUrl",
                table: "Blogs");

            migrationBuilder.DropColumn(
                name: "FocusKeyphrase",
                table: "Blogs");

            migrationBuilder.DropColumn(
                name: "IsPublished",
                table: "Blogs");

            migrationBuilder.DropColumn(
                name: "LastModified",
                table: "Blogs");

            migrationBuilder.DropColumn(
                name: "MetaDescription",
                table: "Blogs");

            migrationBuilder.DropColumn(
                name: "MetaKeywords",
                table: "Blogs");

            migrationBuilder.DropColumn(
                name: "MetaTitle",
                table: "Blogs");

            migrationBuilder.DropColumn(
                name: "NoFollow",
                table: "Blogs");

            migrationBuilder.DropColumn(
                name: "NoIndex",
                table: "Blogs");

            migrationBuilder.DropColumn(
                name: "OgDescription",
                table: "Blogs");

            migrationBuilder.DropColumn(
                name: "OgImage",
                table: "Blogs");

            migrationBuilder.DropColumn(
                name: "OgTitle",
                table: "Blogs");

            migrationBuilder.DropColumn(
                name: "Slug",
                table: "Blogs");

            migrationBuilder.DropColumn(
                name: "ViewCount",
                table: "Blogs");

            migrationBuilder.UpdateData(
                table: "Blogs",
                keyColumn: "BlogId",
                keyValue: 1,
                columns: new[] { "Author", "Content", "Date", "Image", "Title", "Url" },
                values: new object[] { "Author1", "Content of blog post 1.", new DateTime(2025, 2, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "blog1.jpg", "Blog Post 1", "blog1" });

            migrationBuilder.UpdateData(
                table: "Blogs",
                keyColumn: "BlogId",
                keyValue: 2,
                columns: new[] { "Author", "CategoryId", "Content", "Date", "Image", "Title", "Url" },
                values: new object[] { "Author2", 2, "Content of blog post 2.", new DateTime(2025, 3, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "blog2.jpg", "Blog Post 2", "blog2" });

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
    }
}
