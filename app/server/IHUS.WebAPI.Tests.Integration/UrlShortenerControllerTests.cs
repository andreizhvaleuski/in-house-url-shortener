using System.Net;
using Stashbox.AspNetCore.Testing;
using static IHUS.WebAPI.Controllers.UrlShortenerController;

namespace IHUS.WebAPI.Tests.Integration;

public sealed class UrlShortenerControllerTests(StashboxWebApplicationFactory<Program> factory)
        : IClassFixture<StashboxWebApplicationFactory<Program>>
{
    private const string ControllerBaseUri = "/api/UrlShortener/";
    private readonly StashboxWebApplicationFactory<Program> _factory = factory;

    [Fact]
    public async Task CreateShortUrl_ShouldReturnShortUrl_WhichCanBeUsedToAccessActualUrl()
    {
        var client = _factory.StashClient((services, httpClientOptions) =>
        {
        });

        var expectedActualUrl = new Uri("https://example.com");

        var createShortUrlResponseMessage = await client.PostAsJsonAsync(
            ControllerBaseUri,
            new
            {
                actualUrl = expectedActualUrl.ToString(),
            });
        createShortUrlResponseMessage.EnsureSuccessStatusCode();
        var createShortUrlResponseBody = await createShortUrlResponseMessage
            .Content
            .ReadFromJsonAsync<CreateShortUrlSuccessResponse>();

        Assert.NotNull(createShortUrlResponseBody);
        Assert.False(string.IsNullOrWhiteSpace(createShortUrlResponseBody.ShortUrl));

        string getRealByKeyUrl = ControllerBaseUri + createShortUrlResponseBody.ShortUrl;
        var getShortUrlResponseMessage = await client.GetAsync(
            getRealByKeyUrl);
        getShortUrlResponseMessage.EnsureSuccessStatusCode();
        var getShortUrlResponseBody = await getShortUrlResponseMessage
            .Content
            .ReadFromJsonAsync<GetActualUrlSuccessResponse>();

        Assert.NotNull(getShortUrlResponseBody);
        Assert.False(string.IsNullOrWhiteSpace(getShortUrlResponseBody.ActualUrl));
        Assert.Equal(expectedActualUrl, new Uri(getShortUrlResponseBody.ActualUrl));
    }

    [Fact]
    public async Task GetActualUrl_ShouldReturn404NotFound_WhenShortUrlKeyUnknown()
    {
        var client = _factory.StashClient((services, httpClientOptions) =>
        {
        });

        var shortUrlKey = "123456";
        var getRealByKeyUrl = ControllerBaseUri + shortUrlKey;
        var getShortUrlResponseMessage = await client.GetAsync(getRealByKeyUrl);

        Assert.Equal(HttpStatusCode.NotFound, getShortUrlResponseMessage.StatusCode);
        
        var errorResponse = await getShortUrlResponseMessage
            .Content
            .ReadFromJsonAsync<ErrorResponse>();

        Assert.NotNull(errorResponse);
        Assert.False(string.IsNullOrWhiteSpace(errorResponse.Message));
        Assert.Contains(shortUrlKey, errorResponse.Message);
    }
}
