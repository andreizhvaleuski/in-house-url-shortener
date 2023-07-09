using IHUS.Database.Entities;
using IHUS.Domain.Entities;
using IHUS.Domain.Services.Repositories;
//using Microsoft.EntityFrameworkCore;
//using Npgsql;

namespace IHUS.Database.Repositories;

public class ShortenedUrlRepository : IShortenedUrlRepository
{
    private const string UniqueViolation = "23505";

    //private readonly IHUSDbContext _dbContext;

    //public ShortenedUrlRepository(IHUSDbContext context)
    //{
    //    _dbContext = context ?? throw new ArgumentNullException(nameof(context));
    //}

    public async Task CreateAsync(ShortenedUrl shortenedUrl)
    {
        //try
        //{
        //    var entity = new ShortenedUrlEntity
        //    {
        //        ShortUrlKey = shortenedUrl.UrlKey,
        //        ActualUrl = shortenedUrl.ActualUrl
        //    };

        //    _dbContext.ShortenedUrls.Add(entity);

        //    await _dbContext.SaveChangesAsync();
        //}
        //catch (DbUpdateException ex)
        //{
        //    if (ex.InnerException is PostgresException pex
        //        && pex.SqlState == UniqueViolation)
        //    {
        //        throw new DuplicateShortUrlKeyException(shortenedUrl.UrlKey, ex);
        //    }

        //    throw;
        //}
    }

    public async Task<ShortenedUrl?> GetAsync(string shortenedUrlKey)
    {
        //var entity = await _dbContext.ShortenedUrls.FindAsync(shortenedUrlKey);

        //return entity is not null
        //    ? new ShortenedUrl(entity.ShortUrlKey, entity.ActualUrl)
        //    : null;

        return null;
    }
}
