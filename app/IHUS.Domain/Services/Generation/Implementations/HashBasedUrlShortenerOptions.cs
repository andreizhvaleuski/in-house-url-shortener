namespace IHUS.Domain.Services.Generation.Implementations;

public sealed class HashBasedUrlShortenerOptions
{
    public int RetryCount { get; init; } = 3;

    public int TimeoutSeconds { get; init; } = 2;
}
