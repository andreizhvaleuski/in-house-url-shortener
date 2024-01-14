using Stashbox.AspNetCore.Testing;
using static IHUS.WebAPI.Controllers.UrlShortenerController;

namespace IHUS.WebAPI.Tests.Integration;

public sealed class UrlShortenerControllerTests
    : IClassFixture<StashboxWebApplicationFactory<Program>>
{
    private readonly StashboxWebApplicationFactory<Program> _factory;

    public UrlShortenerControllerTests(StashboxWebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateShortUrl_ShouldReturnShortUrl_WhichCanBeUsedToAccessActualUrl()
    {
        var client = _factory.StashClient((services, httpClientOptions) =>
        {
            httpClientOptions.BaseAddress = new Uri("http://localhost/api/UrlShortener/");
        });

        var expectedActualUrl = new Uri("https://example.com");

        var createShortUrlResponseMessage = await client.PostAsJsonAsync(
            "",
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

        var getShortUrlResponseMessage = await client.GetAsync(createShortUrlResponseBody.ShortUrl);
        getShortUrlResponseMessage.EnsureSuccessStatusCode();
        var getShortUrlResponseBody = await getShortUrlResponseMessage
            .Content
            .ReadFromJsonAsync<GetActualUrlSuccessResponse>();

        Assert.NotNull(getShortUrlResponseBody);
        Assert.False(string.IsNullOrWhiteSpace(getShortUrlResponseBody.ActualUrl));
        Assert.Equal(expectedActualUrl, new Uri(getShortUrlResponseBody.ActualUrl));
    }
}
