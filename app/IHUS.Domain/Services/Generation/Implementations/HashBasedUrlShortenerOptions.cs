namespace IHUS.Domain.Services.Generation.Implementations;

public sealed class HashBasedUrlShortenerOptions
{
    public int RetryCount { get; }

    public int TimeoutSeconds { get; }

    public HashBasedUrlShortenerOptions(int retryCount, int timeoutSeconds)
    {
        if (retryCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(retryCount));
        }

        if (timeoutSeconds <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(timeoutSeconds));
        }

        RetryCount = retryCount;
        TimeoutSeconds = timeoutSeconds;
    }
}
