namespace IHUS.Database.Repositories
{
    [Serializable]
    public class DuplicateShortUrlKeyException : Exception
    {
        public DuplicateShortUrlKeyException(string shortUrlKey, Exception innerException)
            : base($"The short URL key '{shortUrlKey}' is already used.", innerException)
        {
            ShortUrlKey = shortUrlKey;
        }

        public string ShortUrlKey { get; }
    }
}
