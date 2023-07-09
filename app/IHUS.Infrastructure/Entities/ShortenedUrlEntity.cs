namespace IHUS.Database.Entities;

public class ShortenedUrlEntity
{
    public string ShortUrlKey { get; set; } = default!;

    public string ActualUrl { get; set; } = default!;
}
