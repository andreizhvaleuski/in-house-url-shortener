using IHUS.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace IHUS.Database
{
    public class IHUSDbContext : DbContext
    {
        public DbSet<ShortenedUrlEntity> ShortenedUrls { get; set; } = default!;

        public IHUSDbContext(DbContextOptions options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder) =>
            modelBuilder.ApplyConfigurationsFromAssembly(GetType().Assembly);
    }
}
