using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Newtonsoft.Json;
using System.Collections.Generic;
using UI.Entity;

namespace UI.Data.Configuration
{
    public class QuizConfiguration : IEntityTypeConfiguration<Quiz>
    {
        public void Configure(EntityTypeBuilder<Quiz> builder)
        {
            builder.ToTable("Quizzes");

            builder.HasKey(q => q.Id);

            builder.Property(q => q.Soru)
                .IsRequired()
                .HasMaxLength(255);

            builder.Property(q => q.Cevap)
                .IsRequired();

            // Configure Text with a Value Converter and Value Comparer
            #nullable disable
            builder.Property(q => q.Text)
                .HasConversion(
                    v => JsonConvert.SerializeObject(v),          // Convert to JSON for storage
                    v => JsonConvert.DeserializeObject<string[]>(v) // Convert back to string[]
                )
                .Metadata.SetValueComparer(
                    new ValueComparer<string[]>(
                        (c1, c2) => c1.SequenceEqual(c2),          // Comparison logic
                        c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())), // Hash code logic
                        c => c.ToArray()                           // Deep copy
                    )
                );

            builder.Property(q => q.Zaman)
                .IsRequired();

            builder.Property(q => q.Date)
                .IsRequired()
                .HasDefaultValueSql("GETDATE()");
        }
    }
}
