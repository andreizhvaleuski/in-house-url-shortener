using IHUS.Domain.Entities;

namespace IHUS.Domain.Services.Generation.Interfaces;

public interface IShortenedUrlGenerator
{
    public Task<ShortenedUrl> GetAsync(string shortUrlKey);

    public Task<ShortenedUrl> GenerateAsync(string actualUrl);

    public Task<ShortenedUrl> GenerateAsync(string shortUrlKey, string actualUrl);
}
