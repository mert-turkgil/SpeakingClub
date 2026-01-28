using Microsoft.EntityFrameworkCore;
using SpeakingClub.Data.Configuration;
using SpeakingClub.Entity;
using SpeakingClub.Identity;

namespace SpeakingClub.Data
{
    public class SpeakingClubContext : DbContext
    {
        public SpeakingClubContext(DbContextOptions<SpeakingClubContext> options)
            : base(options)
        {
        }

        // DbSets for core entities
        public DbSet<Blog> Blogs { get; set; }
        public DbSet<BlogQuiz> BlogQuizzes { get; set; }
        public DbSet<BlogTranslation> BlogTranslations { get; set; }
        public DbSet<Quiz> Quizzes { get; set; }
        public DbSet<Question> Questions {get; set;}
        public DbSet<QuizAnalysis> QuizAnalyses { get; set; }
        public DbSet<QuizAnswer> QuizAnswers { get; set; }
        public DbSet<QuizResponse> QuizResponses { get; set; }
        public DbSet<QuizSubmission> QuizSubmissions { get; set; }
        public DbSet<Word> Words { get; set; }
        
        // DbSets for additional entities
        public DbSet<Category> Categories { get; set; }
        public DbSet<Tag> Tags { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<Rating> Ratings { get; set; }
        public DbSet<UserQuiz> UserQuizzes { get; set; }

        public DbSet<SlideShow> Slide { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            // Ignore the navigation property so that only the foreign key is used in this context.
            // modelBuilder.Entity<QuizSubmission>().Ignore(qs => qs.User);            
            modelBuilder.Entity<QuizSubmission>()
                .Property(qs => qs.UserId)
                .IsRequired();
            // Configure composite key for BlogQuiz join table
            modelBuilder.Entity<BlogQuiz>()
                .HasKey(bq => new { bq.BlogId, bq.QuizId });
            // Configure unique constraint for BlogTranslation
            modelBuilder.Entity<BlogTranslation>()
                .HasIndex(bt => new { bt.BlogId, bt.LanguageCode })
                .IsUnique();
            // Configure cascade delete
            modelBuilder.Entity<BlogTranslation>()
                .HasOne(bt => bt.Blog)
                .WithMany(b => b.Translations)
                .HasForeignKey(bt => bt.BlogId)
                .OnDelete(DeleteBehavior.Cascade);
            // Configure composite key for UserQuiz join table
            modelBuilder.Entity<UserQuiz>()
                .HasKey(uq => new { uq.UserId, uq.QuizId });

            // Configure QuizAnalysis as a keyless entity
            modelBuilder.Entity<QuizAnalysis>()
                .HasNoKey();

            modelBuilder.Entity<Blog>()
                .HasMany(b => b.Tags)
                .WithMany(t => t.Blogs);

            modelBuilder.Entity<Quiz>()
                .HasMany(q => q.Questions)
                .WithOne(qs => qs.Quiz)
                .HasForeignKey(qs => qs.QuizId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Question>()
                .HasMany(q => q.Answers)
                .WithOne(a => a.Question)
                .HasForeignKey(a => a.QuestionId)
                .OnDelete(DeleteBehavior.Cascade);
            // Configure many-to-many relationship between Quiz and Word
            modelBuilder.Entity<Quiz>()
                .HasMany(q => q.Words)
                .WithMany(w => w.Quizzes);

            modelBuilder.Entity<Blog>()
                .HasOne(b => b.Category)
                .WithMany(c => c.Blogs)
                .HasForeignKey(b => b.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Quiz>()
                .HasOne(q => q.Category)
                .WithMany(c => c.Quizzes)
                .HasForeignKey(q => q.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);
            modelBuilder.Entity<SlideShow>().HasKey(s => s.SlideId);
            modelBuilder.Entity<SlideShow>().Property(s => s.CarouselTitle).IsRequired();
            modelBuilder.Entity<SlideShow>().Property(s => s.CarouselImage).IsRequired();
            modelBuilder.Entity<SlideShow>().Property(s=>s.DateAdded).HasDefaultValueSql("GETDATE()");
            modelBuilder.SeedData();
        }
    }
}
