using Force.DeepCloner;
using IHUS.Database.Repositories;
using IHUS.Domain.Entities;
using IHUS.Domain.Services.Generation.Implementations;
using IHUS.Domain.Services.Generation.Interfaces;
using IHUS.Domain.Services.Repositories;
using Moq;
using Polly.Timeout;

namespace IHUS.Domain.Tests.Unit;

public sealed class HashBasedUrlShortenerTests : IDisposable
{
    private readonly Mock<IHashProvider> _hashProviderMock;
    private readonly Mock<ISaltProvider> _saltProviderMock;
    private readonly Mock<IShortenedUrlRepository> _shortenedUrlRepositoryMock;

    private HashBasedUrlShortener BuildHashBasedUrlShortener() =>
        new(hashProvider: _hashProviderMock.Object,
            shortenedUrlRepository: _shortenedUrlRepositoryMock.Object,
            saltProvider: _saltProviderMock.Object);

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

        _shortenedUrlRepositoryMock.Setup(mock => mock.CreateAsync(It.IsAny<ShortenedUrl>()));
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
            () => hashBasedUrlShortener.GenerateAsync(actualUrl));
#pragma warning restore CS8604 // Possible null reference argument.
    }

    [Theory]
    [MemberData(nameof(GetWhiteSpaceArguments), 1)]
    public async Task GenerateAsync_ShouldThrowArgumentException_WhenShortUrlArgumentIsNullOrWhiteSpace(string? shortUrl)
    {
        var hashBasedUrlShortener = BuildHashBasedUrlShortener();

#pragma warning disable CS8604 // Possible null reference argument.
        _ = await Assert.ThrowsAsync<ArgumentException>(
            () => hashBasedUrlShortener.GenerateAsync(shortUrl, "example.com"));
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
            () => hashBasedUrlShortener.GenerateAsync(shortUrl, actualUrl));
#pragma warning restore CS8604 // Possible null reference argument.
    }

    public static IEnumerable<object?[]> GetWhiteSpaceArguments(int argumentsNumber = 1)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(argumentsNumber);

        string?[] whiteSpaces = [null, "", " ", "\t", "\r", "\n", "\n\r"];

        return argumentsNumber switch
        {
            1 => whiteSpaces.Select(whiteSpace => new object?[] { whiteSpace }),
            2 => whiteSpaces.SelectMany(whiteSpace => whiteSpaces
                .Select(ws2 => new object?[] { whiteSpace, ws2 })),
            _ => throw new ArgumentOutOfRangeException(nameof(argumentsNumber))
        };
    }

    [Fact]
    public async Task GenerateAsync_ShouldThrowCantCreateShortenedUrlException_WhenTimeoutRejectedExceptionIsCaught()
    {
        var internalException = new TimeoutRejectedException();

        _shortenedUrlRepositoryMock.Setup(mock => mock.CreateAsync(It.IsAny<ShortenedUrl>()))
            .ThrowsAsync(internalException);

        var hashBasedUrlShortener = BuildHashBasedUrlShortener();

#pragma warning disable CS8604 // Possible null reference argument.
        var wrappedException = await Assert.ThrowsAsync<CantCreateShortenedUrlException>(
            () => hashBasedUrlShortener.GenerateAsync("example.com"));
#pragma warning restore CS8604 // Possible null reference argument.

        Assert.Equal(internalException, wrappedException.InnerException);

        _shortenedUrlRepositoryMock.Verify(mock => mock.CreateAsync(It.IsAny<ShortenedUrl>()), Times.AtLeastOnce);
        _hashProviderMock.Verify(mock => mock.CalculateHash(It.IsAny<byte[]>()), Times.AtLeastOnce);
        _saltProviderMock.Verify(mock => mock.GetSalt(), Times.AtLeastOnce);
    }

    [Fact]
    public async Task GenerateAsync_ShouldThrowCantCreateShortenedUrlException_WhenDuplicateShortUrlKeyExceptionIsCaught()
    {
        var shortUrlKey = "xxxxx";
        var innerException = new Exception("Inner exception");
        var internalException = new DuplicateShortUrlKeyException(shortUrlKey, innerException);

        _shortenedUrlRepositoryMock.Setup(mock => mock.CreateAsync(It.IsAny<ShortenedUrl>()))
            .ThrowsAsync(internalException);

        var hashBasedUrlShortener = BuildHashBasedUrlShortener();

#pragma warning disable CS8604 // Possible null reference argument.
        var wrappedException = await Assert.ThrowsAsync<CantCreateShortenedUrlException>(
            () => hashBasedUrlShortener.GenerateAsync("example.com"));
#pragma warning restore CS8604 // Possible null reference argument.

        Assert.Equal(internalException, wrappedException.InnerException);

        _shortenedUrlRepositoryMock.Verify(mock => mock.CreateAsync(It.IsAny<ShortenedUrl>()), Times.AtLeastOnce);
        _hashProviderMock.Verify(mock => mock.CalculateHash(It.IsAny<byte[]>()), Times.AtLeastOnce);
        _saltProviderMock.Verify(mock => mock.GetSalt(), Times.AtLeastOnce);
    }

    public void Dispose()
    {
        _hashProviderMock.VerifyNoOtherCalls();
        _saltProviderMock.VerifyNoOtherCalls();
        _shortenedUrlRepositoryMock.VerifyNoOtherCalls();
    }
}
