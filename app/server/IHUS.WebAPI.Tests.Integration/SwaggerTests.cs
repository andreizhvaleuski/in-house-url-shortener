using Stashbox.AspNetCore.Testing;

namespace IHUS.WebAPI.Tests.Integration;

public sealed class SwaggerTests(StashboxWebApplicationFactory<Program> factory)
        : IClassFixture<StashboxWebApplicationFactory<Program>>
{
    [Fact]
    public async Task CreateShortUrl_ShouldReturnShortUrl_WhichCanBeUsedToAccessActualUrl()
    {
        var client = factory.StashClient((services, httpClientOptions) =>
        {
            httpClientOptions.BaseAddress = new Uri("http://localhost/");
        });

        var swaggerPage = await client.GetStringAsync("swagger");

        Assert.NotNull(swaggerPage);
    }
}
