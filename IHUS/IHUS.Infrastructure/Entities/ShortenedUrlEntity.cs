using IHUS.Domain.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IHUS.Database.Entities
{
    public class ShortenedUrlEntity
    {
        public string ShortUrlKey { get; set; } = default!;

        public string ActualUrl { get; set; } = default!;
    }

    internal class ShortenedUrlEntityTypeConfiguration
        : IEntityTypeConfiguration<ShortenedUrlEntity>
    {
        public void Configure(EntityTypeBuilder<ShortenedUrlEntity> builder)
        {
            builder.ToTable("ShortenedUrls");

            builder.HasKey(x => x.ShortUrlKey);

            builder.Property(x => x.ShortUrlKey)
                .HasColumnType($"character({Limits.ShortUrlKeyLength})");

            builder.Property(x => x.ActualUrl)
                .HasMaxLength(Limits.ActualUrlMaxLength);
        }
    }
}
