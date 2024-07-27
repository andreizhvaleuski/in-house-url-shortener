using Force.DeepCloner;
using IHUS.Database.Repositories;
using IHUS.Domain.Entities;
using IHUS.Domain.Services.Generation.Implementations;
using IHUS.Domain.Services.Generation.Interfaces;
using IHUS.Domain.Services.Repositories;
using Microsoft.Extensions.Options;
using Moq;
using Polly.Timeout;

namespace IHUS.Domain.Tests.Unit;

public sealed class HashBasedUrlShortenerTests : IDisposable
{
    private readonly Mock<IHashProvider> _hashProviderMock;
    private readonly Mock<ISaltProvider> _saltProviderMock;
    private readonly Mock<IShortenedUrlRepository> _shortenedUrlRepositoryMock;
    private readonly Mock<IOptions<HashBasedUrlShortenerOptions>> _hashBasedUrlShortenerOptionsMock;

    private int _retryCount = 3;
    private int _timeoutSeconds = 2;

    private HashBasedUrlShortener BuildHashBasedUrlShortener() =>
        new(hashProvider: _hashProviderMock.Object,
            shortenedUrlRepository: _shortenedUrlRepositoryMock.Object,
            saltProvider: _saltProviderMock.Object,
            options: _hashBasedUrlShortenerOptionsMock.Object);

    public HashBasedUrlShortenerTests()
    {
        _hashProviderMock = new Mock<IHashProvider>();
        _hashProviderMock.Setup(mock => mock.CalculateHash(It.IsAny<byte[]>()))
            .Returns(Enumerable.Repeat<byte>(1, 128).ToArray());

        _saltProviderMock = new Mock<ISaltProvider>();
        _saltProviderMock.Setup(mock => mock.GetSalt())
            .Returns(Enumerable.Repeat<byte>(2, 128).ToArray());

        _shortenedUrlRepositoryMock = new Mock<IShortenedUrlRepository>();
        _shortenedUrlRepositoryMock.Setup(mock => mock.GetAsync(It.IsAny<string>()))
            .ReturnsAsync(new ShortenedUrl("123xyz", "https://example.com"));
        _shortenedUrlRepositoryMock.Setup(mock => mock.CreateAsync(It.IsAny<ShortenedUrl>(), It.IsAny<CancellationToken>()));

        _hashBasedUrlShortenerOptionsMock = new Mock<IOptions<HashBasedUrlShortenerOptions>>();
        _hashBasedUrlShortenerOptionsMock.SetupGet(mock => mock.Value)
            .Returns(() => new HashBasedUrlShortenerOptions
            { 
                RetryCount = _retryCount,
                TimeoutSeconds = _timeoutSeconds
            });
    }

    [Theory]
    [InlineData(true, false, false, false)]
    [InlineData(true, false, false, true)]
    [InlineData(false, true, false, false)]
    [InlineData(false, true, false, true)]
    [InlineData(false, false, true, false)]
    [InlineData(false, false, true, true)]
    [InlineData(false, true, true, false)]
    [InlineData(false, true, true, true)]
    [InlineData(true, false, true, false)]
    [InlineData(true, false, true, true)]
    [InlineData(true, true, false, false)]
    [InlineData(true, true, false, true)]
    [InlineData(true, true, true, false)]
    [InlineData(true, true, true, true)]
    public void Constructor_ShouldThrowArgumentNullException_WhenAnyArgumentIsNull(
        bool hashProviderIsNull,
        bool shortenedUrlRepositoryIsNull,
        bool saltProviderIsNull,
        bool optionsIsNull)
    {
#pragma warning disable CS8604 // Possible null reference argument.
        Assert.Throws<ArgumentNullException>(
            () => new HashBasedUrlShortener(
                hashProvider: hashProviderIsNull ? null : _hashProviderMock.Object,
                shortenedUrlRepository: shortenedUrlRepositoryIsNull ? null : _shortenedUrlRepositoryMock.Object,
                saltProvider: saltProviderIsNull ? null : _saltProviderMock.Object,
                options: optionsIsNull ? null : _hashBasedUrlShortenerOptionsMock.Object));
#pragma warning restore CS8604 // Possible null reference argument.
    }

    [Fact]
    public async Task GetAsync_ShouldThrowShortenedUrlNotFoundException_WhenShortUrlKeyDoesntExist()
    {
        _shortenedUrlRepositoryMock.Setup(mock => mock.GetAsync("key"))
            .ReturnsAsync((ShortenedUrl?)null);

        var hashBasedUrlShortener = BuildHashBasedUrlShortener();

        await Assert.ThrowsAsync<ShortenedUrlNotFoundException>(
            () => hashBasedUrlShortener.GetAsync("key"));

        _shortenedUrlRepositoryMock.Verify(mock => mock.GetAsync("key"), Times.Once);
    }

    [Fact]
    public async Task GetAsync_ShouldNotWrapExceptions_WhenThrowedByInternals()
    {
        _shortenedUrlRepositoryMock.Setup(mock => mock.GetAsync(It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException());

        var hashBasedUrlShortener = BuildHashBasedUrlShortener();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => hashBasedUrlShortener.GetAsync(It.IsAny<string>()));

        _shortenedUrlRepositoryMock.Verify(mock => mock.GetAsync(It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task GetAsync_ShouldReturnShortenedUrl_WhenValidShortUrlKeyIsPassed()
    {
        var inputShortUrl = "123456";
        var shortenedUrl = new ShortenedUrl(inputShortUrl, "https://example.com");
        var expectedShortenedUrl = shortenedUrl.DeepClone();

        _shortenedUrlRepositoryMock.Setup(mock => mock.GetAsync(inputShortUrl))
            .ReturnsAsync(shortenedUrl.DeepClone());

        var hashBasedUrlShortener = BuildHashBasedUrlShortener();

        var actualShortenedUrl = await hashBasedUrlShortener.GetAsync(inputShortUrl);

        Assert.Equal(expectedShortenedUrl, actualShortenedUrl);

        _shortenedUrlRepositoryMock.Verify(mock => mock.GetAsync(inputShortUrl), Times.Once);
    }

    [Theory]
    [MemberData(nameof(GetWhiteSpaceArguments), 1)]
    public async Task GenerateAsync_ShouldThrowArgumentException_WhenActualUrlArgumentIsNullOrWhiteSpace(string? actualUrl)
    {
        var hashBasedUrlShortener = BuildHashBasedUrlShortener();

#pragma warning disable CS8604 // Possible null reference argument.
        _ = await Assert.ThrowsAsync<ArgumentException>(
            () => hashBasedUrlShortener.GenerateAsync(actualUrl, CancellationToken.None));
#pragma warning restore CS8604 // Possible null reference argument.
    }

    [Theory]
    [MemberData(nameof(GetWhiteSpaceArguments), 1)]
    public async Task GenerateAsync_ShouldThrowArgumentException_WhenShortUrlArgumentIsNullOrWhiteSpace(string? shortUrl)
    {
        var hashBasedUrlShortener = BuildHashBasedUrlShortener();

#pragma warning disable CS8604 // Possible null reference argument.
        _ = await Assert.ThrowsAsync<ArgumentException>(
            () => hashBasedUrlShortener.GenerateAsync(shortUrl, "example.com", CancellationToken.None));
#pragma warning restore CS8604 // Possible null reference argument.
    }

    [Theory]
    [MemberData(nameof(GetWhiteSpaceArguments), 2)]
    public async Task GenerateAsync_ShouldThrowArgumentException_WhenShortUrlOrActualUrlArgumentIsNullOrWhiteSpace(
        string? actualUrl,
        string? shortUrl)
    {
        var hashBasedUrlShortener = BuildHashBasedUrlShortener();

#pragma warning disable CS8604 // Possible null reference argument.
        _ = await Assert.ThrowsAsync<ArgumentException>(
            () => hashBasedUrlShortener.GenerateAsync(shortUrl, actualUrl, CancellationToken.None));
#pragma warning restore CS8604 // Possible null reference argument.
    }

    [Fact]
    public async Task GenerateAsync_ShouldThrowCantCreateShortenedUrlException_WhenTimeoutRejectedExceptionIsCaught()
    {
        var internalException = new TimeoutRejectedException();

        _shortenedUrlRepositoryMock.Setup(mock => mock.CreateAsync(It.IsAny<ShortenedUrl>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(internalException);

        var hashBasedUrlShortener = BuildHashBasedUrlShortener();

#pragma warning disable CS8604 // Possible null reference argument.
        var wrappedException = await Assert.ThrowsAsync<CantCreateShortenedUrlException>(
            () => hashBasedUrlShortener.GenerateAsync("example.com", CancellationToken.None));
#pragma warning restore CS8604 // Possible null reference argument.

        Assert.Equal(internalException, wrappedException.InnerException);

        _shortenedUrlRepositoryMock.Verify(mock => mock.CreateAsync(It.IsAny<ShortenedUrl>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
        _hashProviderMock.Verify(mock => mock.CalculateHash(It.IsAny<byte[]>()), Times.AtLeastOnce);
        _saltProviderMock.Verify(mock => mock.GetSalt(), Times.AtLeastOnce);
    }

    [Fact]
    public async Task GenerateAsync_ShouldThrowCantCreateShortenedUrlException_WhenDuplicateShortUrlKeyExceptionIsCaught()
    {
        var shortUrlKey = "xxxxx";
        var innerException = new Exception("Inner exception");
        var internalException = new DuplicateShortUrlKeyException(shortUrlKey, innerException);

        _shortenedUrlRepositoryMock.Setup(mock => mock.CreateAsync(It.IsAny<ShortenedUrl>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(internalException);

        var hashBasedUrlShortener = BuildHashBasedUrlShortener();

#pragma warning disable CS8604 // Possible null reference argument.
        var wrappedException = await Assert.ThrowsAsync<CantCreateShortenedUrlException>(
            () => hashBasedUrlShortener.GenerateAsync("example.com", CancellationToken.None));
#pragma warning restore CS8604 // Possible null reference argument.

        Assert.Equal(internalException, wrappedException.InnerException);

        _shortenedUrlRepositoryMock.Verify(mock => mock.CreateAsync(It.IsAny<ShortenedUrl>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
        _hashProviderMock.Verify(mock => mock.CalculateHash(It.IsAny<byte[]>()), Times.AtLeastOnce);
        _saltProviderMock.Verify(mock => mock.GetSalt(), Times.AtLeastOnce);
    }

    [Fact]
    public async Task GenerateAsync_ShouldRetryBasedOnRetryOption_WhenDuplicateShortUrlKeyExceptionIsThrown()
    {
        var shortUrlKey = "xxxxx";
        var innerException = new Exception("Inner exception");
        var internalException = new DuplicateShortUrlKeyException(shortUrlKey, innerException);

        var shortenedUrlRepositoryMockSetupSequence = _shortenedUrlRepositoryMock
            .SetupSequence(mock => mock.CreateAsync(It.IsAny<ShortenedUrl>(), It.IsAny<CancellationToken>()));

        for (int i = 0; i < _retryCount; i++)
        {
            shortenedUrlRepositoryMockSetupSequence.ThrowsAsync(internalException);
        }

        shortenedUrlRepositoryMockSetupSequence.Returns(Task.CompletedTask);

        var hashBasedUrlShortener = BuildHashBasedUrlShortener();
        await hashBasedUrlShortener.GenerateAsync("https://example.com", CancellationToken.None);

        var executionNumber = _retryCount + 1;

        _shortenedUrlRepositoryMock.Verify(mock => mock.CreateAsync(It.IsAny<ShortenedUrl>(), It.IsAny<CancellationToken>()), Times.Exactly(executionNumber));
        _hashProviderMock.Verify(mock => mock.CalculateHash(It.IsAny<byte[]>()), Times.Exactly(executionNumber));
        _saltProviderMock.Verify(mock => mock.GetSalt(), Times.Exactly(executionNumber));
    }

    [Fact]
    public async Task GenerateAsync_ShouldNotRetryBasedOnRetryOption_WhenNotDuplicateShortUrlKeyExceptionIsThrown()
    {
        var exception = new Exception("Inner exception");

        _shortenedUrlRepositoryMock.Setup(mock => mock.CreateAsync(It.IsAny<ShortenedUrl>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        var hashBasedUrlShortener = BuildHashBasedUrlShortener();
        await Assert.ThrowsAsync<Exception>(
            () => hashBasedUrlShortener.GenerateAsync("https://example.com", CancellationToken.None));

        _shortenedUrlRepositoryMock.Verify(mock => mock.CreateAsync(It.IsAny<ShortenedUrl>(), It.IsAny<CancellationToken>()), Times.Once);
        _hashProviderMock.Verify(mock => mock.CalculateHash(It.IsAny<byte[]>()), Times.Once);
        _saltProviderMock.Verify(mock => mock.GetSalt(), Times.Once);
    }

    [Fact]
    public async Task GenerateAsync_ShouldTimeotBasedOnTimeoutOptions_WhenExecutionTakesMoreTimeThanAllowed()
    {
        _shortenedUrlRepositoryMock.Setup(mock => mock.CreateAsync(It.IsAny<ShortenedUrl>(), It.IsAny<CancellationToken>()))
            .Returns<ShortenedUrl, CancellationToken>((_, cancellationToken) => Task.Delay(TimeSpan.FromSeconds(_timeoutSeconds + 2), cancellationToken));

        var hashBasedUrlShortener = BuildHashBasedUrlShortener();
        var exception = await Assert.ThrowsAsync<CantCreateShortenedUrlException>(
            () => hashBasedUrlShortener.GenerateAsync("https://example.com", CancellationToken.None));

        Assert.IsType<TimeoutRejectedException>(exception.InnerException);

        _shortenedUrlRepositoryMock.Verify(mock => mock.CreateAsync(It.IsAny<ShortenedUrl>(), It.IsAny<CancellationToken>()), Times.Between(0, int.MaxValue, Moq.Range.Inclusive));
        _hashProviderMock.Verify(mock => mock.CalculateHash(It.IsAny<byte[]>()), Times.Between(0, int.MaxValue, Moq.Range.Inclusive));
        _saltProviderMock.Verify(mock => mock.GetSalt(), Times.Between(0, int.MaxValue, Moq.Range.Inclusive));
    }

    public static IEnumerable<object?[]> GetWhiteSpaceArguments(int argumentsNumber = 1)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(argumentsNumber);

        string?[] whiteSpaces = [null, "", '\u00A0'.ToString(), " ", "\t", "\r", "\n", "\n\r"];

        return argumentsNumber switch
        {
            1 => whiteSpaces.Select(whiteSpace => new object?[] { whiteSpace }),
            2 => whiteSpaces.SelectMany(whiteSpace => whiteSpaces
                .Select(ws2 => new object?[] { whiteSpace, ws2 })),
            _ => throw new ArgumentOutOfRangeException(nameof(argumentsNumber))
        };
    }

    public void Dispose()
    {
        _hashProviderMock.VerifyNoOtherCalls();
        _saltProviderMock.VerifyNoOtherCalls();
        _shortenedUrlRepositoryMock.VerifyNoOtherCalls();
    }
}
