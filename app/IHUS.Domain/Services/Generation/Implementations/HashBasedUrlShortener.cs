using IHUS.Database.Repositories;
using IHUS.Domain.Constants;
using IHUS.Domain.Entities;
using IHUS.Domain.Services.Generation.Interfaces;
using IHUS.Domain.Services.Repositories;
using Polly;
using Polly.Timeout;
using System.Text;

namespace IHUS.Domain.Services.Generation.Implementations;

public sealed class HashBasedUrlShortener : IShortenedUrlGenerator
{
    private const int RetryCount = 3;
    private const int TimeoutSeconds = 2;

    private readonly IHashProvider _hashProvider;
    private readonly IShortenedUrlRepository _shortenedUrlRepository;
    private readonly ISaltProvider _saltProvider;

    public HashBasedUrlShortener(
        IHashProvider hashProvider,
        IShortenedUrlRepository shortenedUrlRepository,
        ISaltProvider saltProvider)
    {
        _hashProvider = hashProvider
            ?? throw new ArgumentNullException(nameof(hashProvider));
        _shortenedUrlRepository = shortenedUrlRepository
            ?? throw new ArgumentNullException(nameof(shortenedUrlRepository));
        _saltProvider = saltProvider;
    }

    public async Task<ShortenedUrl> GetAsync(string shortUrlKey)
    {
        var shortenedUrl = await _shortenedUrlRepository.GetAsync(shortUrlKey);

        return shortenedUrl is null
            ? throw new ShortenedUrlNotFoundException(shortUrlKey)
            : shortenedUrl;
    }

    public async Task<ShortenedUrl> GenerateAsync(string actualUrl)
    {
        ValidateActualUrl(actualUrl);

        var timeoutPolicy = Policy.TimeoutAsync(TimeoutSeconds, TimeoutStrategy.Pessimistic);
        var exceptionPolicy = Policy
            .Handle<DuplicateShortUrlKeyException>()
            .RetryAsync(RetryCount);

        var executionPolicy = Policy.WrapAsync(timeoutPolicy, exceptionPolicy);

        try
        {
            return await executionPolicy.ExecuteAsync(
                () => GenerateInternalAsync(actualUrl));
        }
        catch (Exception ex)
        {
            switch (ex)
            {
                case TimeoutRejectedException:
                case DuplicateShortUrlKeyException:
                    throw new CantCreateShortenedUrlException(actualUrl, ex);

                default:
                    throw;
            }
        }
    }

    private async Task<ShortenedUrl> GenerateInternalAsync(string actualUrl)
    {
        var shortUrlKey = GenerateShortUrlKey(actualUrl);
        var shortenedUrl = new ShortenedUrl(shortUrlKey, actualUrl);

        await _shortenedUrlRepository.CreateAsync(shortenedUrl);

        return shortenedUrl;
    }

    private string GenerateShortUrlKey(string actualUrl)
    {
        var bytes = Encoding.UTF8.GetBytes(actualUrl)
            .Concat(_saltProvider.GetSalt())
            .ToArray();
        var hash = _hashProvider.CalculateHash(bytes);
        var base64Hash = Convert.ToBase64String(hash);
        var shourtUrlKey = base64Hash[..Limits.ShortUrlKeyLength];

        return shourtUrlKey;
    }

    public async Task<ShortenedUrl> GenerateAsync(string shortUrlKey, string actualUrl)
    {
        ValidateShortUrlKey(shortUrlKey);
        ValidateActualUrl(actualUrl);

        var shortenedUrl = new ShortenedUrl(shortUrlKey, actualUrl);

        await _shortenedUrlRepository.CreateAsync(shortenedUrl);

        return shortenedUrl;
    }

    private static void ValidateActualUrl(string actualUrl)
    {
        if (string.IsNullOrWhiteSpace(actualUrl))
        {
            throw new ArgumentException(
                $"'{nameof(actualUrl)}' cannot be null or whitespace.",
                nameof(actualUrl));
        }
    }

    private static void ValidateShortUrlKey(string shortUrlKey)
    {
        if (string.IsNullOrWhiteSpace(shortUrlKey))
        {
            throw new ArgumentException(
                $"'{nameof(shortUrlKey)}' cannot be null or whitespace.",
                nameof(shortUrlKey));
        }

        if (shortUrlKey.Length != Limits.ShortUrlKeyLength)
        {
            throw new ArgumentOutOfRangeException(nameof(shortUrlKey));
        }
    }
}
