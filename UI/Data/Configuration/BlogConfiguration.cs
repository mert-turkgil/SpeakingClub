using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UI.Entity;

namespace UI.Data.Configuration
{
public class BlogConfiguration : IEntityTypeConfiguration<Blog>
    {
        public void Configure(EntityTypeBuilder<Blog> builder)
        {
            // Specify the table name (optional)
            builder.ToTable("Blogs");

            // Set the primary key
            builder.HasKey(b => b.Id);

            // Configure properties
            builder.Property(b => b.Title)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(b => b.Content)
                .IsRequired();

            builder.Property(b => b.Author)
                .HasMaxLength(100);

            builder.Property(b => b.DateCreated)
                .HasDefaultValueSql("GETUTCDATE()");

            // Index on DateCreated for faster queries
            builder.HasIndex(b => b.DateCreated);
        }
    }
}