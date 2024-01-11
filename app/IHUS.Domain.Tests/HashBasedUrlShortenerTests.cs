using Force.DeepCloner;
using IHUS.Database.Repositories;
using IHUS.Domain.Entities;
using IHUS.Domain.Services.Generation.Implementations;
using IHUS.Domain.Services.Generation.Interfaces;
using IHUS.Domain.Services.Repositories;
using Moq;

namespace IHUS.Domain.Tests.Unit;

public sealed class HashBasedUrlShortenerTests
{
    [Fact]
    public async Task GetAsync_ShouldThrowShortenedUrlNotFoundException_WhenShortUrlKeyDoesntExist()
    {
        var hashProviderMock = new Mock<IHashProvider>();
        var saltProviderMock = new Mock<ISaltProvider>();

        var shortenedUrlRepositoryMock = new Mock<IShortenedUrlRepository>();
        shortenedUrlRepositoryMock.Setup(mock => mock.GetAsync("key"))
            .ReturnsAsync((ShortenedUrl?)null);

        var hashBasedUrlShortener = new HashBasedUrlShortener(
            hashProviderMock.Object,
            shortenedUrlRepositoryMock.Object,
            saltProviderMock.Object);

        await Assert.ThrowsAsync<ShortenedUrlNotFoundException>(() => hashBasedUrlShortener.GetAsync("key"));

        shortenedUrlRepositoryMock.Verify(mock => mock.GetAsync("key"), Times.Once);
        shortenedUrlRepositoryMock.VerifyNoOtherCalls();
        hashProviderMock.VerifyNoOtherCalls();
        saltProviderMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetAsync_ShouldNotCatchExceptions_ThrowedByInternals()
    {
        var hashProviderMock = new Mock<IHashProvider>();
        var saltProviderMock = new Mock<ISaltProvider>();

        var shortenedUrlRepositoryMock = new Mock<IShortenedUrlRepository>();
        shortenedUrlRepositoryMock.Setup(mock => mock.GetAsync(It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException());

        var hashBasedUrlShortener = new HashBasedUrlShortener(
            hashProviderMock.Object,
            shortenedUrlRepositoryMock.Object,
            saltProviderMock.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(() => hashBasedUrlShortener.GetAsync(It.IsAny<string>()));

        shortenedUrlRepositoryMock.Verify(mock => mock.GetAsync(It.IsAny<string>()), Times.Once);
        shortenedUrlRepositoryMock.VerifyNoOtherCalls();
        hashProviderMock.VerifyNoOtherCalls();
        saltProviderMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetAsyncShouldReturnShortenedUrlWhenValidShortUrlKeyIsPassed()
    {
        var hashProviderMock = new Mock<IHashProvider>();
        var saltProviderMock = new Mock<ISaltProvider>();

        var inputShortUrl = "123456";
        var shortenedUrl = new ShortenedUrl(inputShortUrl, "https://example.com");
        var expectedShortenedUrl = shortenedUrl.DeepClone();

        var shortenedUrlRepositoryMock = new Mock<IShortenedUrlRepository>();
        shortenedUrlRepositoryMock.Setup(mock => mock.GetAsync(inputShortUrl))
            .ReturnsAsync(shortenedUrl.DeepClone());

        var hashBasedUrlShortener = new HashBasedUrlShortener(
            hashProviderMock.Object,
            shortenedUrlRepositoryMock.Object,
            saltProviderMock.Object);

        var actualShortenedUrl = await hashBasedUrlShortener.GetAsync(inputShortUrl);

        Assert.Equal(expectedShortenedUrl, actualShortenedUrl);

        shortenedUrlRepositoryMock.Verify(mock => mock.GetAsync(inputShortUrl), Times.Once);
        shortenedUrlRepositoryMock.VerifyNoOtherCalls();
        hashProviderMock.VerifyNoOtherCalls();
        saltProviderMock.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task GenerateAsyncShouldNotAllowNullOrWhiteSpaceActualUrlArgument(string? actualUrl)
    {
        var shortenedUrlRepositoryMock = new Mock<IShortenedUrlRepository>();
        var hashProviderMock = new Mock<IHashProvider>();
        var saltProviderMock = new Mock<ISaltProvider>();

        var hashBasedUrlShortener = new HashBasedUrlShortener(
            hashProviderMock.Object,
            shortenedUrlRepositoryMock.Object,
            saltProviderMock.Object);

#pragma warning disable CS8604 // Possible null reference argument.
        _ = await Assert.ThrowsAsync<ArgumentException>(
            () => hashBasedUrlShortener.GenerateAsync(actualUrl));
#pragma warning restore CS8604 // Possible null reference argument.

        shortenedUrlRepositoryMock.VerifyNoOtherCalls();
        hashProviderMock.VerifyNoOtherCalls();
        saltProviderMock.VerifyNoOtherCalls();
    }
}
