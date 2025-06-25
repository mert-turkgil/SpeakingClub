using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SpeakingClub.Entity;
using SpeakingClub.Identity;

namespace SpeakingClub.Data.Configuration
{
    public static class ModelBuilderExtensions
    {
        public static void SeedData(this ModelBuilder modelBuilder)
        {
            var user1Id = "user1-id";
            var user2Id = "user2-id";
                        // Seed Users
            modelBuilder.Entity<User>().HasData(
                    new User
                    {
                        Id = "teacher1",
                        UserName = "teacher1@example.com",
                        NormalizedUserName = "TEACHER1@EXAMPLE.COM",
                        Email = "teacher1@example.com",
                        FirstName = "Ali",
                        LastName = "Öğretmen",
                        Age = 30,
                        NormalizedEmail = "TEACHER1@EXAMPLE.COM",
                        EmailConfirmed = true,
                        PasswordHash = "FakeHash",
                        SecurityStamp = "securitystamp1"
                    },
                new User { Id = user1Id,FirstName = "Mert", LastName = "Yılmaz",Age=22, UserName = "user1@example.com", NormalizedUserName = "USER1@EXAMPLE.COM", Email = "user1@example.com", NormalizedEmail = "USER1@EXAMPLE.COM", EmailConfirmed = true, PasswordHash = "FakeHash", SecurityStamp = Guid.NewGuid().ToString() },
                new User { Id = user2Id,FirstName = "Zeynep",LastName = "Demir",Age=18, UserName = "user2@example.com", NormalizedUserName = "USER2@EXAMPLE.COM", Email = "user2@example.com", NormalizedEmail = "USER2@EXAMPLE.COM", EmailConfirmed = true, PasswordHash = "FakeHash", SecurityStamp = Guid.NewGuid().ToString() }
            );
            // Seed SlideShow
            modelBuilder.Entity<SlideShow>().HasData(
                new SlideShow
                {
                    SlideId = 1,
                    CarouselTitle = "Slide 1",
                    CarouselImage = "slide1.jpg",
                    CarouselImage600w = "slide1_600w.jpg",
                    CarouselImage1200w = "slide1_1200w.jpg",
                    CarouselDescription = "Description for slide 1",
                    CarouselLink = "http://example.com/slide1",
                    CarouselLinkText = "Learn More",
                    DateAdded = new DateTime(2025, 1, 1)
                },
                new SlideShow
                {
                    SlideId = 2,
                    CarouselTitle = "Slide 2",
                    CarouselImage = "slide2.jpg",
                    CarouselImage600w = "slide2_600w.jpg",
                    CarouselImage1200w = "slide2_1200w.jpg",
                    CarouselDescription = "Description for slide 2",
                    CarouselLink = "http://example.com/slide2",
                    CarouselLinkText = "Learn More",
                    DateAdded = new DateTime(2025, 1, 15)
                }
            );

            // Seed Categories
            modelBuilder.Entity<Category>().HasData(
                new Category { CategoryId = 1, Name = "General" },
                new Category { CategoryId = 2, Name = "Science" },
                new Category { CategoryId = 3, Name = "Technology" }
            );

            // Seed Tags
            modelBuilder.Entity<Tag>().HasData(
                new Tag { TagId = 1, Name = "Grammar" },
                new Tag { TagId = 2, Name = "Vocabulary" },
                new Tag { TagId = 3, Name = "Listening" },
                new Tag { TagId = 4, Name = "Reading" },
                new Tag { TagId = 5, Name = "Writing" }
            );


            // Seed Blogs
            modelBuilder.Entity<Blog>().HasData(
                new Blog
                {
                    BlogId = 1,
                    Title = "Blog Post 1",
                    Content = "Content of blog post 1.",
                    Url = "blog1",
                    Image = "blog1.jpg",
                    Date = new DateTime(2025, 2, 1),
                    Author = "Author1",
                    RawYT = "",
                    RawMaps = "",
                    isHome = true,
                    CategoryId = 1
                },
                new Blog
                {
                    BlogId = 2,
                    Title = "Blog Post 2",
                    Content = "Content of blog post 2.",
                    Url = "blog2",
                    Image = "blog2.jpg",
                    Date = new DateTime(2025, 3, 1),
                    Author = "Author2",
                    RawYT = "",
                    RawMaps = "",
                    isHome = true,
                    CategoryId = 2
                }
            );

            // Seed a single Quiz with multiple Questions
            modelBuilder.Entity<Quiz>().HasData(
                new Quiz
                {
                    Id = 1,
                    Title = "General Knowledge Quiz",
                    Description = "A quiz to test your general knowledge.",
                    AudioUrl = "http://example.com/audio1.mp3",
                    YouTubeVideoUrl = "http://youtube.com/vid1",
                    TeacherId = "teacher1",
                    CategoryId = 1
                }
            );

            // Seed Questions for the Quiz
            modelBuilder.Entity<Question>().HasData(
                new Question
                {
                    Id = 1,
                    QuizId = 1,
                    QuestionText = "What is 2 + 2?",
                    ImageUrl = null,
                    AudioUrl = null,
                    VideoUrl = null
                },
                new Question
                {
                    Id = 2,
                    QuizId = 1,
                    QuestionText = "What is the capital of France?",
                    ImageUrl = "http://example.com/images/france.jpg",
                    AudioUrl = null,
                    VideoUrl = null
                },
                new Question
                {
                    Id = 3,
                    QuizId = 1,
                    QuestionText = "Identify the instrument in the audio clip.",
                    ImageUrl = null,
                    AudioUrl = "http://example.com/audio/instrument.mp3",
                    VideoUrl = null
                },
                new Question
                {
                    Id = 4,
                    QuizId = 1,
                    QuestionText = "Watch the video and answer: Who is the speaker?",
                    ImageUrl = null,
                    AudioUrl = null,
                    VideoUrl = "http://youtube.com/watch?v=example"
                }
            );

            // Seed QuizAnswers for each Question
            modelBuilder.Entity<QuizAnswer>().HasData(
                // Answers for Question 1: What is 2+2?
                new QuizAnswer { Id = 1, QuestionId = 1, AnswerText = "4", IsCorrect = true },
                new QuizAnswer { Id = 2, QuestionId = 1, AnswerText = "3", IsCorrect = false },
                new QuizAnswer { Id = 3, QuestionId = 1, AnswerText = "5", IsCorrect = false },

                // Answers for Question 2: What is the capital of France?
                new QuizAnswer { Id = 4, QuestionId = 2, AnswerText = "Paris", IsCorrect = true },
                new QuizAnswer { Id = 5, QuestionId = 2, AnswerText = "Berlin", IsCorrect = false },
                new QuizAnswer { Id = 6, QuestionId = 2, AnswerText = "Madrid", IsCorrect = false },

                // Answers for Question 3: Identify the instrument in the audio clip.
                new QuizAnswer { Id = 7, QuestionId = 3, AnswerText = "Piano", IsCorrect = true },
                new QuizAnswer { Id = 8, QuestionId = 3, AnswerText = "Guitar", IsCorrect = false },
                new QuizAnswer { Id = 9, QuestionId = 3, AnswerText = "Violin", IsCorrect = false },

                // Answers for Question 4: Who is the speaker?
                new QuizAnswer { Id = 10, QuestionId = 4, AnswerText = "Dr. Smith", IsCorrect = true },
                new QuizAnswer { Id = 11, QuestionId = 4, AnswerText = "Mr. Johnson", IsCorrect = false },
                new QuizAnswer { Id = 12, QuestionId = 4, AnswerText = "Ms. Davis", IsCorrect = false }
            );

            // Update BlogQuiz join table to reference the only quiz (Id = 1)
            modelBuilder.Entity<BlogQuiz>().HasData(
                new BlogQuiz { BlogId = 1, QuizId = 1 }
            );

            // Seed QuizSubmissions (all referencing quiz with Id = 1)
            modelBuilder.Entity<QuizSubmission>().HasData(
                new QuizSubmission
                {
                    QuizSubmissionId = 1,
                    QuizId = 1,
                    UserId = user1Id,
                    SubmissionDate = DateTime.UtcNow.AddDays(-2),
                    Score = 100,
                    AttemptNumber = 1
                },
                new QuizSubmission
                {
                    QuizSubmissionId = 2,
                    QuizId = 1,
                    UserId = user1Id,
                    SubmissionDate = DateTime.UtcNow.AddDays(-1),
                    Score = 50,
                    AttemptNumber = 2
                },
                new QuizSubmission
                {
                    QuizSubmissionId = 3,
                    QuizId = 1,
                    UserId = user2Id,
                    SubmissionDate = DateTime.UtcNow.AddDays(-1),
                    Score = 40,
                    AttemptNumber = 1
                }
            );
            // Seed Words
            modelBuilder.Entity<Word>().HasData(
                new Word
                {
                    WordId = 1,
                    Term = "Aberration",
                    Definition = "A deviation from the norm",
                    Example = "The color aberration was noticeable.",
                    IsFromApi = false
                },
                new Word
                {
                    WordId = 2,
                    Term = "Benevolent",
                    Definition = "Kind and generous",
                    Example = "She had a benevolent smile.",
                    IsFromApi = false
                }
            );


            // Seed QuizResponses (example response)
            modelBuilder.Entity<QuizResponse>().HasData(
                // user1 attempt 1 (QuizSubmissionId = 1), all correct answers
                new QuizResponse { QuizResponseId = 1, QuizSubmissionId = 1, QuizAnswerId = 1, TimeTakenSeconds = 10, AnswerText = "4" },          // Q1
                new QuizResponse { QuizResponseId = 2, QuizSubmissionId = 1, QuizAnswerId = 4, TimeTakenSeconds = 15, AnswerText = "Paris" },      // Q2
                new QuizResponse { QuizResponseId = 3, QuizSubmissionId = 1, QuizAnswerId = 7, TimeTakenSeconds = 20, AnswerText = "Piano" },      // Q3
                new QuizResponse { QuizResponseId = 4, QuizSubmissionId = 1, QuizAnswerId = 10, TimeTakenSeconds = 18, AnswerText = "Dr. Smith" }, // Q4

                // user1 attempt 2 (QuizSubmissionId = 2), some correct, some wrong
                new QuizResponse { QuizResponseId = 5, QuizSubmissionId = 2, QuizAnswerId = 2, TimeTakenSeconds = 12, AnswerText = "3" },          // Q1 (wrong)
                new QuizResponse { QuizResponseId = 6, QuizSubmissionId = 2, QuizAnswerId = 4, TimeTakenSeconds = 17, AnswerText = "Paris" },      // Q2 (correct)
                new QuizResponse { QuizResponseId = 7, QuizSubmissionId = 2, QuizAnswerId = 8, TimeTakenSeconds = 25, AnswerText = "Guitar" },     // Q3 (wrong)
                new QuizResponse { QuizResponseId = 8, QuizSubmissionId = 2, QuizAnswerId = 10, TimeTakenSeconds = 22, AnswerText = "Dr. Smith" },  // Q4 (correct)

                // user2 attempt 1 (QuizSubmissionId = 3)
                new QuizResponse { QuizResponseId = 9, QuizSubmissionId = 3, QuizAnswerId = 2, TimeTakenSeconds = 12, AnswerText = "3" },          // Q1 (wrong)
                new QuizResponse { QuizResponseId = 10, QuizSubmissionId = 3, QuizAnswerId = 4, TimeTakenSeconds = 17, AnswerText = "Paris" },      // Q2 (correct)
                new QuizResponse { QuizResponseId = 11, QuizSubmissionId = 3, QuizAnswerId = 8, TimeTakenSeconds = 25, AnswerText = "Guitar" },     // Q3 (wrong)
                new QuizResponse { QuizResponseId = 12, QuizSubmissionId = 3, QuizAnswerId = 10, TimeTakenSeconds = 22, AnswerText = "Dr. Smith" }             
            );

            // Seed many-to-many join data for Blog-Tags
            modelBuilder.Entity("BlogTag").HasData(
                new { BlogsBlogId = 1, TagsTagId = 2 },
                new { BlogsBlogId = 2, TagsTagId = 4 }
            );

            // Seed many-to-many join data for Quiz-Tags (only for Quiz Id = 1)
            modelBuilder.Entity("QuizTag").HasData(
                new { QuizzesId = 1, TagsTagId = 1 }
            );

            // Seed many-to-many join data for Quiz-Words (only for Quiz Id = 1)
            modelBuilder.Entity("QuizWord").HasData(
                new { QuizzesId = 1, WordsWordId = 1 },
                new { QuizzesId = 1, WordsWordId = 2 }
            );

            // Seed UserQuiz join table (User-Quiz relationship)
            modelBuilder.Entity<UserQuiz>().HasData(
                new UserQuiz { UserId = user1Id, QuizId = 1, TotalAttempts = 2 },
                new UserQuiz { UserId = user2Id, QuizId = 1, TotalAttempts = 1 }
            );

        }
    }
}
