using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UI.Entity;

namespace Data.Configuration
{
    public class CarouselConfiguration : IEntityTypeConfiguration<Carousel>
    {
        public void Configure(EntityTypeBuilder<Carousel> builder)
        {
            builder.HasKey(m => m.CarouselId);
            builder.Property(m => m.CarouselImage)
                   .IsRequired()
                   .HasMaxLength(255); // Optional: Set max length if appropriate
            builder.Property(m => m.DateAdded)
                   .HasDefaultValueSql("GETDATE()"); // Default value for SQL Server
        }
    }
}
