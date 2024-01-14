using IHUS.Database.Repositories;
using IHUS.Domain.Constants;
using IHUS.Domain.Entities;
using IHUS.Domain.Services.Generation.Interfaces;
using IHUS.Domain.Services.Repositories;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Timeout;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace IHUS.Domain.Services.Generation.Implementations;

public sealed class HashBasedUrlShortener : IShortenedUrlGenerator
{
    private readonly IHashProvider _hashProvider;
    private readonly IShortenedUrlRepository _shortenedUrlRepository;
    private readonly ISaltProvider _saltProvider;
    private readonly HashBasedUrlShortenerOptions _options;

    public HashBasedUrlShortener(
        IHashProvider hashProvider,
        IShortenedUrlRepository shortenedUrlRepository,
        ISaltProvider saltProvider,
        IOptions<HashBasedUrlShortenerOptions> options)
    {
        _hashProvider = hashProvider
            ?? throw new ArgumentNullException(nameof(hashProvider));
        _shortenedUrlRepository = shortenedUrlRepository
            ?? throw new ArgumentNullException(nameof(shortenedUrlRepository));
        _saltProvider = saltProvider
            ?? throw new ArgumentNullException(nameof(saltProvider));
        _options = options?.Value
            ?? throw new ArgumentNullException(nameof(options));
    }

    public async Task<ShortenedUrl> GetAsync(string shortUrlKey)
    {
        var shortenedUrl = await _shortenedUrlRepository.GetAsync(shortUrlKey);

        return shortenedUrl is null
            ? throw new ShortenedUrlNotFoundException(shortUrlKey)
            : shortenedUrl;
    }

    public async Task<ShortenedUrl> GenerateAsync([NotNull] string actualUrl)
    {
        ValidateActualUrl(actualUrl);

        var timeoutPolicy = Policy.TimeoutAsync(_options.TimeoutSeconds, TimeoutStrategy.Pessimistic);
        var exceptionPolicy = Policy
            .Handle<DuplicateShortUrlKeyException>()
            .RetryAsync(_options.RetryCount);

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
