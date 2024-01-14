using Stashbox.AspNetCore.Testing;

namespace IHUS.WebAPI.Tests.Integration;

public sealed class SwaggerTests
    : IClassFixture<StashboxWebApplicationFactory<Program>>
{
    private readonly StashboxWebApplicationFactory<Program> _factory;

    public SwaggerTests(StashboxWebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateShortUrl_ShouldReturnShortUrl_WhichCanBeUsedToAccessActualUrl()
    {
        var client = _factory.StashClient((services, httpClientOptions) =>
        {
            httpClientOptions.BaseAddress = new Uri("http://localhost/");
        });

        var swaggerPage = await client.GetStringAsync("swagger");

        Assert.NotNull(swaggerPage);
    }
}
