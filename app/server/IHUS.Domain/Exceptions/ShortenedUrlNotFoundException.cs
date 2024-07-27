namespace IHUS.Database.Repositories;

[Serializable]
public class ShortenedUrlNotFoundException : Exception
{
    public ShortenedUrlNotFoundException(string shortUrlKey)
        : base($"The short URL with the '{shortUrlKey}' key not found.")
    {
        ShortUrlKey = shortUrlKey;
    }

    public string ShortUrlKey { get; }
}
