namespace IHUS.Domain.Entities;

/// <summary>
/// Shortened URL entity.
/// </summary>
/// <param name="UrlKey">Actual resource URL key used to reference it. Can be referred as a short URL.</param>
/// <param name="ActualUrl">Actual resource URL.</param>
public record class ShortenedUrl(string UrlKey, string ActualUrl);
