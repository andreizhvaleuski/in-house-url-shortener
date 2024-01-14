using System.Globalization;
using IHUS.Database.Repositories;
using IHUS.Domain.Services.Generation.Implementations;
using IHUS.Domain.Services.Generation.Interfaces;
using IHUS.Domain.Services.Repositories;
using IHUS.WebAPI.Infrastructure.Extensions;
using Serilog;

namespace IHUS.WebAPI;

public class Program
{
    public static async Task Main(string[] args)
    {
        var app = BuildApp(args);

        ConfigureMiddlewarePipeline(app);

        await app.RunAsync();
    }

    private static WebApplication BuildApp(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder
            .Services
                .AddControllers()
                .AddControllersAsServices()
            .Services
                .AddEndpointsApiExplorer()
                .AddSwaggerGen();

        builder
            .Services.AddNpgsqlDataSource(builder.Configuration.GetRequiredConnectionString("Default"));

        builder.Services
            .AddSingleton<IHashProvider, Sha256HashProvider>()
            .AddSingleton<ISaltProvider, RngSaltProvider>()
            .AddScoped<IShortenedUrlRepository, ShortenedUrlRepository>()
            .AddScoped<IShortenedUrlGenerator, HashBasedUrlShortener>()
            .Configure<HashBasedUrlShortenerOptions>(
                builder.Configuration.GetSection("HashBasedUrlShortener"));

        builder.Services
            .AddHealthChecks()
            .AddNpgSql(
                npgsqlConnectionString: builder.Configuration.GetRequiredConnectionString("HealthCheck"),
                name: "PostgreSQL",
                tags: new[] { "db", "sql", "postgresql" });

        builder.Host.UseSerilog((context, services, configuration) => configuration
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext()
            .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture)
            .WriteTo.Debug(formatProvider: CultureInfo.InvariantCulture));

        builder.Host.UseStashbox(container =>
        {
            container.Configure(configurator =>
            {
                configurator.WithLifetimeValidation();
                configurator.WithDisposableTransientTracking();
            });
        });

        return builder.Build();
    }

    private static void ConfigureMiddlewarePipeline(WebApplication app)
    {
        app.UseSwagger();
        app.UseSwaggerUI();
        app.UseHealthChecksPrometheusExporter("/health-metrics");
        app.UseSerilogRequestLogging();
        app.UseHttpsRedirection();
        app.UseAuthorization();
        app.MapControllers();
    }
}
