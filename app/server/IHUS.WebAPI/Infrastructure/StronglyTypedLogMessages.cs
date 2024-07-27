using IHUS.Database.Repositories;

namespace IHUS.WebAPI;

internal static partial class StronglyTypedLogMessages
{
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Short URL can't be created")]
    public static partial void LogCantCreateShortenedUrlException(this ILogger logger, CantCreateShortenedUrlException exception);
}
