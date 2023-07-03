using IHUS.Domain.Entities;

namespace IHUS.Domain.Services.Repositories;

public interface IShortenedUrlRepository
{
    public Task<ShortenedUrl?> GetAsync(string shortenedUrlKey);

    public Task CreateAsync(ShortenedUrl shortenedUrl);
}
