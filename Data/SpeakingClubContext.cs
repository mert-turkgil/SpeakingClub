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

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
            // Suppress pending model changes warning after fixing dynamic seed data
            optionsBuilder.ConfigureWarnings(warnings =>
                warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
        }

        // DbSets for core entities
        public DbSet<Blog> Blogs { get; set; }
        public DbSet<BlogQuiz> BlogQuizzes { get; set; }
        public DbSet<BlogTranslation> BlogTranslations { get; set; }
        public DbSet<BlogFile> BlogFiles { get; set; }
        public DbSet<Quiz> Quizzes { get; set; }
        public DbSet<Question> Questions {get; set;}
        public DbSet<QuizAnalysis> QuizAnalyses { get; set; }
        public DbSet<QuizAnswer> QuizAnswers { get; set; }
        public DbSet<QuizResponse> QuizResponses { get; set; }
        public DbSet<QuizSubmission> QuizSubmissions { get; set; }
        public DbSet<QuizAttempt> QuizAttempts { get; set; }
        public DbSet<Word> Words { get; set; }
        
        // DbSets for additional entities
        public DbSet<Category> Categories { get; set; }
        public DbSet<Tag> Tags { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<Rating> Ratings { get; set; }
        public DbSet<UserQuiz> UserQuizzes { get; set; }

        public DbSet<SlideShow> Slide { get; set; }
        public DbSet<DownloadLog> DownloadLogs { get; set; }

        // E-mails (Entity/EmailEntities.cs, Services/Email)
        public DbSet<EmailLog> EmailLogs { get; set; }
        public DbSet<EmailOutbox> EmailOutbox { get; set; }
        public DbSet<EmailCampaign> EmailCampaigns { get; set; }

        // Read-only (Entity/ReportProjections.cs)
        public DbSet<ReportUser> ReportUsers { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            // Ignore the navigation property so that only the foreign key is used in this context.
            // modelBuilder.Entity<QuizSubmission>().Ignore(qs => qs.User);            
            // 450 = the length of an Identity user id; nvarchar(max) could not be indexed
            modelBuilder.Entity<QuizSubmission>()
                .Property(qs => qs.UserId)
                .IsRequired()
                .HasMaxLength(450);
            modelBuilder.Entity<QuizSubmission>()
                .Property(qs => qs.EndReason)
                .HasMaxLength(20);
            // One submission per exam attempt (makes double submits harmless)
            modelBuilder.Entity<QuizSubmission>()
                .HasIndex(qs => qs.AttemptId)
                .IsUnique()
                .HasFilter("[AttemptId] IS NOT NULL");
            // Reports filter by date first (Services/Reports): this index covers the result list and its statistics
            modelBuilder.Entity<QuizSubmission>()
                .HasIndex(qs => qs.SubmissionDate)
                .IncludeProperties(qs => new { qs.QuizId, qs.UserId, qs.Score, qs.DurationSeconds, qs.ViolationCount, qs.EndReason, qs.AttemptNumber });
            modelBuilder.Entity<QuizSubmission>()
                .HasIndex(qs => new { qs.QuizId, qs.SubmissionDate })
                .IncludeProperties(qs => new { qs.UserId, qs.Score, qs.AttemptNumber });
            modelBuilder.Entity<QuizSubmission>()
                .HasIndex(qs => new { qs.UserId, qs.QuizId, qs.SubmissionDate })
                .IncludeProperties(qs => qs.Score);
            modelBuilder.Entity<QuizResponse>()
                .HasIndex(r => r.QuestionId);
            // Question statistics read every response of a set of submissions: the columns they need come with the index
            modelBuilder.Entity<QuizResponse>()
                .HasIndex(r => r.QuizSubmissionId)
                .IncludeProperties(r => new { r.QuestionId, r.QuizAnswerId, r.IsCorrect, r.TimeTakenSeconds });
            // The background sweep that collects attempts whose time ran out
            modelBuilder.Entity<QuizAttempt>()
                .HasIndex(a => new { a.SubmittedAt, a.ExpiresAt });
            // Students' names for the exam report (Entity/ReportProjections.cs): read-only, not in migrations
            modelBuilder.Entity<ReportUser>()
                .HasNoKey()
                .ToTable((string?)null)   // read through the query below only: no table, no migration
                .ToSqlQuery("SELECT Id AS UserId, FirstName, LastName, Email FROM AspNetUsers");
            modelBuilder.Entity<DownloadLog>()
                .HasIndex(dl => new { dl.UserId, dl.DownloadedAt });

            // E-mails: the outbox is read by "what is due next", the log by date and status (Admin → E-postalar)
            modelBuilder.Entity<EmailOutbox>(e =>
            {
                e.Property(x => x.DedupeKey).HasMaxLength(200);
                e.Property(x => x.Template).HasMaxLength(50);
                e.Property(x => x.RecipientEmail).HasMaxLength(256);
                e.Property(x => x.UserId).HasMaxLength(450);
                e.Property(x => x.Culture).HasMaxLength(10);
                e.Property(x => x.LastError).HasMaxLength(1000);
                e.Property(x => x.RelatedType).HasMaxLength(30);
                e.HasIndex(x => x.DedupeKey).IsUnique();
                e.HasIndex(x => new { x.Category, x.NextAttemptAt }).HasFilter("[ProcessedAt] IS NULL");
                e.HasIndex(x => x.CampaignId);
                e.HasIndex(x => x.UserId);
            });
            modelBuilder.Entity<EmailLog>(e =>
            {
                e.Property(x => x.Template).HasMaxLength(50);
                e.Property(x => x.RecipientEmail).HasMaxLength(256);
                e.Property(x => x.UserId).HasMaxLength(450);
                e.Property(x => x.Subject).HasMaxLength(300);
                e.Property(x => x.Error).HasMaxLength(1000);
                e.Property(x => x.RelatedType).HasMaxLength(30);
                e.HasIndex(x => x.CreatedAt);
                e.HasIndex(x => new { x.Status, x.CreatedAt });
                e.HasIndex(x => x.SentAt);
                e.HasIndex(x => x.CampaignId);
                e.HasIndex(x => x.UserId);
            });
            modelBuilder.Entity<EmailCampaign>(e =>
            {
                e.Property(x => x.Kind).HasMaxLength(20);
                e.Property(x => x.Title).HasMaxLength(300);
                e.Property(x => x.CreatedBy).HasMaxLength(450);
                e.Property(x => x.Cursor).HasMaxLength(450);
                e.HasIndex(x => new { x.Kind, x.EntityId }).IsUnique();
            });

            // Identity.User is also mapped here (a separate "User" table, through the Comment/Rating/UserQuiz
            // navigations). The member fields belong to AspNetUsers only (ApplicationDbContext), so this context
            // ignores them and its model stays unchanged.
            modelBuilder.Entity<User>(user =>
            {
                user.Ignore(u => u.CreatedAt);
                user.Ignore(u => u.LastLoginAt);
                user.Ignore(u => u.PreferredCulture);
                user.Ignore(u => u.EmailOptOuts);
                user.Ignore(u => u.PromoConsentAt);
                user.Ignore(u => u.PromoConsentSource);
                user.Ignore(u => u.EmailPreferencesToken);
            });

            modelBuilder.Entity<QuizAttempt>(attempt =>
            {
                attempt.HasKey(a => a.Id);
                attempt.Property(a => a.UserId).IsRequired().HasMaxLength(450);
                attempt.HasIndex(a => new { a.UserId, a.QuizId, a.SubmittedAt });
                attempt.HasOne(a => a.Quiz)
                    .WithMany()
                    .HasForeignKey(a => a.QuizId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
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
            // Configure BlogFile cascade delete
            modelBuilder.Entity<BlogFile>()
                .HasOne(bf => bf.Blog)
                .WithMany(b => b.Files)
                .HasForeignKey(bf => bf.BlogId)
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

            // Configure DownloadLog
            modelBuilder.Entity<DownloadLog>()
                .HasOne(dl => dl.BlogFile)
                .WithMany()
                .HasForeignKey(dl => dl.BlogFileId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.SeedData();
        }
    }
}
