using Dapper;
using IHUS.Database.Entities;
using IHUS.Domain.Entities;
using IHUS.Domain.Services.Repositories;
using Npgsql;

namespace IHUS.Database.Repositories;

public class ShortenedUrlRepository : IShortenedUrlRepository
{
    private const string UniqueViolation = "23505";

    private readonly NpgsqlConnection _npgsqlConnection;

    public ShortenedUrlRepository(NpgsqlConnection npgsqlConnection)
    {
        _npgsqlConnection = npgsqlConnection ?? throw new ArgumentNullException(nameof(npgsqlConnection));
    }

    public async Task CreateAsync(ShortenedUrl shortenedUrl, CancellationToken cancellationToken)
    {
        try
        {
            await _npgsqlConnection.ExecuteAsync(new CommandDefinition(
                commandText:
@"INSERT INTO ""public"".""ShortenedUrls"" VALUES
(@ShortUrlKey, @ActualUrl)",
                parameters: new
                { 
                    ShortUrlKey = shortenedUrl.UrlKey,
                    ActualUrl = shortenedUrl.ActualUrl },
                cancellationToken: cancellationToken));
        }
        catch (PostgresException ex)
        when (ex.SqlState == UniqueViolation)
        {
            throw new DuplicateShortUrlKeyException(shortenedUrl.UrlKey, ex);
        }
    }

    public async Task<ShortenedUrl?> GetAsync(string shortenedUrlKey)
    {
        var entity = await _npgsqlConnection.QueryFirstOrDefaultAsync<ShortenedUrlEntity>(@"
SELECT ""ShortUrlKey"", ""ActualUrl""
FROM ""public"".""ShortenedUrls"" AS ""su""
WHERE ""su"".""ShortUrlKey"" = @shortenedUrlKey",
new { shortenedUrlKey });

        return entity is not null
            ? new ShortenedUrl(entity.ShortUrlKey, entity.ActualUrl)
            : null;
    }
}
