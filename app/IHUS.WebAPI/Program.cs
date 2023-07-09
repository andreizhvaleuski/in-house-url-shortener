using System.Globalization;
using IHUS.Database.Repositories;
using IHUS.Domain.Services.Generation.Implementations;
using IHUS.Domain.Services.Generation.Interfaces;
using IHUS.Domain.Services.Repositories;
using Serilog;

namespace IHUS.WebAPI;

public class Program
{
    public static async Task Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .Enrich.FromLogContext()
            .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture)
            .WriteTo.Debug(formatProvider: CultureInfo.InvariantCulture)
            .CreateBootstrapLogger();

        try
        {
            var app = BuildApp(args);

            ConfigureMiddlewares(app);

            await app.RunAsync();
        }
        catch (Exception exception)
        {
            Log.Fatal(exception, "Application terminated unexpectedly");
        }
        finally
        {
            await Log.CloseAndFlushAsync();
        }
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
            .Services.AddNpgsqlDataSource(builder.Configuration.GetConnectionString("Default"));

        builder.Services
            .AddSingleton<IHashProvider, Sha256HashProvider>()
            .AddSingleton<ISaltProvider, RngSaltProvider>()
            .AddScoped<IShortenedUrlRepository, ShortenedUrlRepository>()
            .AddScoped<IShortenedUrlGenerator, HashBasedUrlShortener>();

        builder.Services
            .AddHealthChecks()
            .AddNpgSql(
                npgsqlConnectionString: builder.Configuration.GetConnectionString("HealthCheck"),
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

    private static void ConfigureMiddlewares(WebApplication app)
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
