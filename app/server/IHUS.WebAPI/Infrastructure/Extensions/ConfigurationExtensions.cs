namespace IHUS.WebAPI.Infrastructure.Extensions;

internal static class ConfigurationExtensions
{
    public static string GetRequiredConnectionString(this IConfiguration configuration, string name)
    {
        var connectionString = configuration?.GetSection("ConnectionStrings")[name];
        ArgumentException.ThrowIfNullOrEmpty(connectionString);
        return connectionString;
    }
}
