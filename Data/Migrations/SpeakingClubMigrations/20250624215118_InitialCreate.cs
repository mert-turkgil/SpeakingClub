using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SpeakingClub.Data.Migrations.SpeakingClubMigrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    CategoryId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.CategoryId);
                });

            migrationBuilder.CreateTable(
                name: "QuizAnalyses",
                columns: table => new
                {
                    QuizId = table.Column<int>(type: "int", nullable: false),
                    TotalSubmissions = table.Column<int>(type: "int", nullable: false),
                    AverageScore = table.Column<double>(type: "float", nullable: false)
                },
                constraints: table =>
                {
                });

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

            migrationBuilder.CreateTable(
                name: "Tags",
                columns: table => new
                {
                    TagId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tags", x => x.TagId);
                });

            migrationBuilder.CreateTable(
                name: "User",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FirstName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Age = table.Column<int>(type: "int", nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NormalizedUserName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SecurityStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "bit", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_User", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Words",
                columns: table => new
                {
                    WordId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Term = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Definition = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Example = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Pronunciation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Synonyms = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Origin = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsFromApi = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Words", x => x.WordId);
                });

            migrationBuilder.CreateTable(
                name: "Blogs",
                columns: table => new
                {
                    BlogId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Url = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Image = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Author = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RawYT = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RawMaps = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    isHome = table.Column<bool>(type: "bit", nullable: false),
                    CategoryId = table.Column<int>(type: "int", nullable: true),
                    SelectedQuestionId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Blogs", x => x.BlogId);
                    table.ForeignKey(
                        name: "FK_Blogs_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "CategoryId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Quizzes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AudioUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    YouTubeVideoUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ImageUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TeacherId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    CategoryId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Quizzes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Quizzes_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "CategoryId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Quizzes_User_TeacherId",
                        column: x => x.TeacherId,
                        principalTable: "User",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "BlogTag",
                columns: table => new
                {
                    BlogsBlogId = table.Column<int>(type: "int", nullable: false),
                    TagsTagId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BlogTag", x => new { x.BlogsBlogId, x.TagsTagId });
                    table.ForeignKey(
                        name: "FK_BlogTag_Blogs_BlogsBlogId",
                        column: x => x.BlogsBlogId,
                        principalTable: "Blogs",
                        principalColumn: "BlogId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BlogTag_Tags_TagsTagId",
                        column: x => x.TagsTagId,
                        principalTable: "Tags",
                        principalColumn: "TagId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BlogQuiz",
                columns: table => new
                {
                    BlogsBlogId = table.Column<int>(type: "int", nullable: false),
                    QuizId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BlogQuiz", x => new { x.BlogsBlogId, x.QuizId });
                    table.ForeignKey(
                        name: "FK_BlogQuiz_Blogs_BlogsBlogId",
                        column: x => x.BlogsBlogId,
                        principalTable: "Blogs",
                        principalColumn: "BlogId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BlogQuiz_Quizzes_QuizId",
                        column: x => x.QuizId,
                        principalTable: "Quizzes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BlogQuizzes",
                columns: table => new
                {
                    BlogId = table.Column<int>(type: "int", nullable: false),
                    QuizId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BlogQuizzes", x => new { x.BlogId, x.QuizId });
                    table.ForeignKey(
                        name: "FK_BlogQuizzes_Blogs_BlogId",
                        column: x => x.BlogId,
                        principalTable: "Blogs",
                        principalColumn: "BlogId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BlogQuizzes_Quizzes_QuizId",
                        column: x => x.QuizId,
                        principalTable: "Quizzes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Comments",
                columns: table => new
                {
                    CommentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    BlogId = table.Column<int>(type: "int", nullable: true),
                    QuizId = table.Column<int>(type: "int", nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Comments", x => x.CommentId);
                    table.ForeignKey(
                        name: "FK_Comments_Blogs_BlogId",
                        column: x => x.BlogId,
                        principalTable: "Blogs",
                        principalColumn: "BlogId");
                    table.ForeignKey(
                        name: "FK_Comments_Quizzes_QuizId",
                        column: x => x.QuizId,
                        principalTable: "Quizzes",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Comments_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Questions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    QuestionText = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ImageUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AudioUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    VideoUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    QuizId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Questions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Questions_Quizzes_QuizId",
                        column: x => x.QuizId,
                        principalTable: "Quizzes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "QuizSubmissions",
                columns: table => new
                {
                    QuizSubmissionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    QuizId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SubmissionDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Score = table.Column<int>(type: "int", nullable: false),
                    AttemptNumber = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuizSubmissions", x => x.QuizSubmissionId);
                    table.ForeignKey(
                        name: "FK_QuizSubmissions_Quizzes_QuizId",
                        column: x => x.QuizId,
                        principalTable: "Quizzes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "QuizTag",
                columns: table => new
                {
                    QuizzesId = table.Column<int>(type: "int", nullable: false),
                    TagsTagId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuizTag", x => new { x.QuizzesId, x.TagsTagId });
                    table.ForeignKey(
                        name: "FK_QuizTag_Quizzes_QuizzesId",
                        column: x => x.QuizzesId,
                        principalTable: "Quizzes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_QuizTag_Tags_TagsTagId",
                        column: x => x.TagsTagId,
                        principalTable: "Tags",
                        principalColumn: "TagId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "QuizWord",
                columns: table => new
                {
                    QuizzesId = table.Column<int>(type: "int", nullable: false),
                    WordsWordId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuizWord", x => new { x.QuizzesId, x.WordsWordId });
                    table.ForeignKey(
                        name: "FK_QuizWord_Quizzes_QuizzesId",
                        column: x => x.QuizzesId,
                        principalTable: "Quizzes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_QuizWord_Words_WordsWordId",
                        column: x => x.WordsWordId,
                        principalTable: "Words",
                        principalColumn: "WordId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Ratings",
                columns: table => new
                {
                    RatingId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Score = table.Column<int>(type: "int", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    BlogId = table.Column<int>(type: "int", nullable: true),
                    QuizId = table.Column<int>(type: "int", nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ratings", x => x.RatingId);
                    table.ForeignKey(
                        name: "FK_Ratings_Blogs_BlogId",
                        column: x => x.BlogId,
                        principalTable: "Blogs",
                        principalColumn: "BlogId");
                    table.ForeignKey(
                        name: "FK_Ratings_Quizzes_QuizId",
                        column: x => x.QuizId,
                        principalTable: "Quizzes",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Ratings_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "UserQuizzes",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    QuizId = table.Column<int>(type: "int", nullable: false),
                    TotalAttempts = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserQuizzes", x => new { x.UserId, x.QuizId });
                    table.ForeignKey(
                        name: "FK_UserQuizzes_Quizzes_QuizId",
                        column: x => x.QuizId,
                        principalTable: "Quizzes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserQuizzes_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "QuizAnswers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    QuestionId = table.Column<int>(type: "int", nullable: false),
                    AnswerText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsCorrect = table.Column<bool>(type: "bit", nullable: false),
                    QuizId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuizAnswers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuizAnswers_Questions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "Questions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_QuizAnswers_Quizzes_QuizId",
                        column: x => x.QuizId,
                        principalTable: "Quizzes",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "QuizResponses",
                columns: table => new
                {
                    QuizResponseId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    QuizSubmissionId = table.Column<int>(type: "int", nullable: false),
                    QuizAnswerId = table.Column<int>(type: "int", nullable: true),
                    AnswerText = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TimeTakenSeconds = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuizResponses", x => x.QuizResponseId);
                    table.ForeignKey(
                        name: "FK_QuizResponses_QuizAnswers_QuizAnswerId",
                        column: x => x.QuizAnswerId,
                        principalTable: "QuizAnswers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_QuizResponses_QuizSubmissions_QuizSubmissionId",
                        column: x => x.QuizSubmissionId,
                        principalTable: "QuizSubmissions",
                        principalColumn: "QuizSubmissionId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Categories",
                columns: new[] { "CategoryId", "Name" },
                values: new object[,]
                {
                    { 1, "General" },
                    { 2, "Science" },
                    { 3, "Technology" }
                });

            migrationBuilder.InsertData(
                table: "Slide",
                columns: new[] { "SlideId", "CarouselDescription", "CarouselImage", "CarouselImage1200w", "CarouselImage600w", "CarouselLink", "CarouselLinkText", "CarouselTitle", "DateAdded" },
                values: new object[,]
                {
                    { 1, "Description for slide 1", "slide1.jpg", "slide1_1200w.jpg", "slide1_600w.jpg", "https://example.com/slide1", "Learn More", "Slide 1", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 2, "Description for slide 2", "slide2.jpg", "slide2_1200w.jpg", "slide2_600w.jpg", "https://example.com/slide2", "Learn More", "Slide 2", new DateTime(2025, 1, 15, 0, 0, 0, 0, DateTimeKind.Unspecified) }
                });

            migrationBuilder.InsertData(
                table: "Tags",
                columns: new[] { "TagId", "Name" },
                values: new object[,]
                {
                    { 1, "Grammar" },
                    { 2, "Vocabulary" },
                    { 3, "Listening" },
                    { 4, "Reading" },
                    { 5, "Writing" }
                });

            migrationBuilder.InsertData(
                table: "User",
                columns: new[] { "Id", "AccessFailedCount", "Age", "ConcurrencyStamp", "Email", "EmailConfirmed", "FirstName", "LastName", "LockoutEnabled", "LockoutEnd", "NormalizedEmail", "NormalizedUserName", "PasswordHash", "PhoneNumber", "PhoneNumberConfirmed", "SecurityStamp", "TwoFactorEnabled", "UserName" },
                values: new object[,]
                {
                    { "teacher1", 0, 30, "c07f7abd-8a55-4d6a-b5a0-e410d58627b4", "teacher1@example.com", true, "Ali", "Öğretmen", false, null, "TEACHER1@EXAMPLE.COM", "TEACHER1@EXAMPLE.COM", "FakeHash", null, false, "securitystamp1", false, "teacher1@example.com" },
                    { "user1-id", 0, 22, "462b3d69-2d86-4a73-86f7-7d6c15286750", "user1@example.com", true, "Mert", "Yılmaz", false, null, "USER1@EXAMPLE.COM", "USER1@EXAMPLE.COM", "FakeHash", null, false, "3004496c-f638-455a-a5cc-0b41a436f1b9", false, "user1@example.com" },
                    { "user2-id", 0, 18, "cec18065-53b4-4fe9-ba10-7fa10126d1c8", "user2@example.com", true, "Zeynep", "Demir", false, null, "USER2@EXAMPLE.COM", "USER2@EXAMPLE.COM", "FakeHash", null, false, "997f70b1-bd26-4b76-94ee-ed43942c0bc1", false, "user2@example.com" }
                });

            migrationBuilder.InsertData(
                table: "Words",
                columns: new[] { "WordId", "Definition", "Example", "IsFromApi", "Origin", "Pronunciation", "Synonyms", "Term" },
                values: new object[,]
                {
                    { 1, "A deviation from the norm", "The color aberration was noticeable.", false, null, null, null, "Aberration" },
                    { 2, "Kind and generous", "She had a benevolent smile.", false, null, null, null, "Benevolent" }
                });

            migrationBuilder.InsertData(
                table: "Blogs",
                columns: new[] { "BlogId", "Author", "CategoryId", "Content", "Date", "Image", "RawMaps", "RawYT", "SelectedQuestionId", "Title", "Url", "isHome" },
                values: new object[,]
                {
                    { 1, "Author1", 1, "Content of blog post 1.", new DateTime(2025, 2, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "blog1.jpg", "", "", null, "Blog Post 1", "https://example.com/blog1", true },
                    { 2, "Author2", 2, "Content of blog post 2.", new DateTime(2025, 3, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "blog2.jpg", "", "", null, "Blog Post 2", "https://example.com/blog2", true }
                });

            migrationBuilder.InsertData(
                table: "Quizzes",
                columns: new[] { "Id", "AudioUrl", "CategoryId", "Description", "ImageUrl", "TeacherId", "Title", "YouTubeVideoUrl" },
                values: new object[] { 1, "https://example.com/audio1.mp3", 1, "A quiz to test your general knowledge.", null, "teacher1", "General Knowledge Quiz", "https://youtube.com/vid1" });

            migrationBuilder.InsertData(
                table: "BlogQuizzes",
                columns: new[] { "BlogId", "QuizId" },
                values: new object[] { 1, 1 });

            migrationBuilder.InsertData(
                table: "BlogTag",
                columns: new[] { "BlogsBlogId", "TagsTagId" },
                values: new object[,]
                {
                    { 1, 2 },
                    { 2, 4 }
                });

            migrationBuilder.InsertData(
                table: "Questions",
                columns: new[] { "Id", "AudioUrl", "ImageUrl", "QuestionText", "QuizId", "VideoUrl" },
                values: new object[,]
                {
                    { 1, null, null, "What is 2 + 2?", 1, null },
                    { 2, null, "https://example.com/images/france.jpg", "What is the capital of France?", 1, null },
                    { 3, "https://example.com/audio/instrument.mp3", null, "Identify the instrument in the audio clip.", 1, null },
                    { 4, null, null, "Watch the video and answer: Who is the speaker?", 1, "https://youtube.com/watch?v=example" }
                });

            migrationBuilder.InsertData(
                table: "QuizSubmissions",
                columns: new[] { "QuizSubmissionId", "AttemptNumber", "QuizId", "Score", "SubmissionDate", "UserId" },
                values: new object[,]
                {
                    { 1, 1, 1, 100, new DateTime(2025, 6, 22, 21, 51, 17, 616, DateTimeKind.Utc).AddTicks(591), "user1-id" },
                    { 2, 2, 1, 50, new DateTime(2025, 6, 23, 21, 51, 17, 616, DateTimeKind.Utc).AddTicks(598), "user1-id" },
                    { 3, 1, 1, 40, new DateTime(2025, 6, 23, 21, 51, 17, 616, DateTimeKind.Utc).AddTicks(609), "user2-id" }
                });

            migrationBuilder.InsertData(
                table: "QuizTag",
                columns: new[] { "QuizzesId", "TagsTagId" },
                values: new object[] { 1, 1 });

            migrationBuilder.InsertData(
                table: "QuizWord",
                columns: new[] { "QuizzesId", "WordsWordId" },
                values: new object[,]
                {
                    { 1, 1 },
                    { 1, 2 }
                });

            migrationBuilder.InsertData(
                table: "UserQuizzes",
                columns: new[] { "QuizId", "UserId", "TotalAttempts" },
                values: new object[,]
                {
                    { 1, "user1-id", 2 },
                    { 1, "user2-id", 1 }
                });

            migrationBuilder.InsertData(
                table: "QuizAnswers",
                columns: new[] { "Id", "AnswerText", "IsCorrect", "QuestionId", "QuizId" },
                values: new object[,]
                {
                    { 1, "4", true, 1, null },
                    { 2, "3", false, 1, null },
                    { 3, "5", false, 1, null },
                    { 4, "Paris", true, 2, null },
                    { 5, "Berlin", false, 2, null },
                    { 6, "Madrid", false, 2, null },
                    { 7, "Piano", true, 3, null },
                    { 8, "Guitar", false, 3, null },
                    { 9, "Violin", false, 3, null },
                    { 10, "Dr. Smith", true, 4, null },
                    { 11, "Mr. Johnson", false, 4, null },
                    { 12, "Ms. Davis", false, 4, null }
                });

            migrationBuilder.InsertData(
                table: "QuizResponses",
                columns: new[] { "QuizResponseId", "AnswerText", "QuizAnswerId", "QuizSubmissionId", "TimeTakenSeconds" },
                values: new object[,]
                {
                    { 1, "4", 1, 1, 10 },
                    { 2, "Paris", 4, 1, 15 },
                    { 3, "Piano", 7, 1, 20 },
                    { 4, "Dr. Smith", 10, 1, 18 },
                    { 5, "3", 2, 2, 12 },
                    { 6, "Paris", 4, 2, 17 },
                    { 7, "Guitar", 8, 2, 25 },
                    { 8, "Dr. Smith", 10, 2, 22 },
                    { 9, "3", 2, 3, 12 },
                    { 10, "Paris", 4, 3, 17 },
                    { 11, "Guitar", 8, 3, 25 },
                    { 12, "Dr. Smith", 10, 3, 22 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_BlogQuiz_QuizId",
                table: "BlogQuiz",
                column: "QuizId");

            migrationBuilder.CreateIndex(
                name: "IX_BlogQuizzes_QuizId",
                table: "BlogQuizzes",
                column: "QuizId");

            migrationBuilder.CreateIndex(
                name: "IX_Blogs_CategoryId",
                table: "Blogs",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_BlogTag_TagsTagId",
                table: "BlogTag",
                column: "TagsTagId");

            migrationBuilder.CreateIndex(
                name: "IX_Comments_BlogId",
                table: "Comments",
                column: "BlogId");

            migrationBuilder.CreateIndex(
                name: "IX_Comments_QuizId",
                table: "Comments",
                column: "QuizId");

            migrationBuilder.CreateIndex(
                name: "IX_Comments_UserId",
                table: "Comments",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Questions_QuizId",
                table: "Questions",
                column: "QuizId");

            migrationBuilder.CreateIndex(
                name: "IX_QuizAnswers_QuestionId",
                table: "QuizAnswers",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_QuizAnswers_QuizId",
                table: "QuizAnswers",
                column: "QuizId");

            migrationBuilder.CreateIndex(
                name: "IX_QuizResponses_QuizAnswerId",
                table: "QuizResponses",
                column: "QuizAnswerId");

            migrationBuilder.CreateIndex(
                name: "IX_QuizResponses_QuizSubmissionId",
                table: "QuizResponses",
                column: "QuizSubmissionId");

            migrationBuilder.CreateIndex(
                name: "IX_QuizSubmissions_QuizId",
                table: "QuizSubmissions",
                column: "QuizId");

            migrationBuilder.CreateIndex(
                name: "IX_QuizTag_TagsTagId",
                table: "QuizTag",
                column: "TagsTagId");

            migrationBuilder.CreateIndex(
                name: "IX_QuizWord_WordsWordId",
                table: "QuizWord",
                column: "WordsWordId");

            migrationBuilder.CreateIndex(
                name: "IX_Quizzes_CategoryId",
                table: "Quizzes",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Quizzes_TeacherId",
                table: "Quizzes",
                column: "TeacherId");

            migrationBuilder.CreateIndex(
                name: "IX_Ratings_BlogId",
                table: "Ratings",
                column: "BlogId");

            migrationBuilder.CreateIndex(
                name: "IX_Ratings_QuizId",
                table: "Ratings",
                column: "QuizId");

            migrationBuilder.CreateIndex(
                name: "IX_Ratings_UserId",
                table: "Ratings",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserQuizzes_QuizId",
                table: "UserQuizzes",
                column: "QuizId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BlogQuiz");

            migrationBuilder.DropTable(
                name: "BlogQuizzes");

            migrationBuilder.DropTable(
                name: "BlogTag");

            migrationBuilder.DropTable(
                name: "Comments");

            migrationBuilder.DropTable(
                name: "QuizAnalyses");

            migrationBuilder.DropTable(
                name: "QuizResponses");

            migrationBuilder.DropTable(
                name: "QuizTag");

            migrationBuilder.DropTable(
                name: "QuizWord");

            migrationBuilder.DropTable(
                name: "Ratings");

            migrationBuilder.DropTable(
                name: "Slide");

            migrationBuilder.DropTable(
                name: "UserQuizzes");

            migrationBuilder.DropTable(
                name: "QuizAnswers");

            migrationBuilder.DropTable(
                name: "QuizSubmissions");

            migrationBuilder.DropTable(
                name: "Tags");

            migrationBuilder.DropTable(
                name: "Words");

            migrationBuilder.DropTable(
                name: "Blogs");

            migrationBuilder.DropTable(
                name: "Questions");

            migrationBuilder.DropTable(
                name: "Quizzes");

            migrationBuilder.DropTable(
                name: "Categories");

            migrationBuilder.DropTable(
                name: "User");
        }
    }
}
