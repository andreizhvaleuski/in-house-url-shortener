namespace IHUS.Database.Repositories;

[Serializable]
public class CantCreateShortenedUrlException : Exception
{
    public CantCreateShortenedUrlException(
        string url,
        Exception innerException)
        : base(
            $"The short URL for the '{url}' URL can't be created.",
            innerException)
    {
        Url = url;
    }

    public string Url { get; }
}
