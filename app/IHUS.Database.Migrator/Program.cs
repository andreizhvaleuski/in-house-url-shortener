using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using FluentMigrator.Runner;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Spectre.Console.Cli;

internal class Program
{
    static int Main(string[] args)
    {
        var app = new CommandApp<MigrateUpDbCommand>();
        return app.Run(args);
    }

    /// <summary>
    /// Configure the dependency injection services
    /// </summary>
    private static ServiceProvider CreateServices(string connectionString)
    {
        return new ServiceCollection()
            // Add common FluentMigrator services
            .AddFluentMigratorCore()
            .ConfigureRunner(rb => rb
                .AddPostgres()
                .WithGlobalConnectionString(connectionString)
                .ScanIn(typeof(Initial).Assembly).For.Migrations())
            // Enable logging to console in the FluentMigrator way
            .AddLogging(lb => lb.AddFluentMigratorConsole())
            .AddSerilog(configurator =>
            {
                configurator.Enrich.FromLogContext();
                configurator.WriteTo.Console(formatProvider: CultureInfo.InvariantCulture);
            })
            // Build the service provider
            .BuildServiceProvider(true);
    }

    /// <summary>
    /// Update the database
    /// </summary>
    private static void UpdateDatabase(IServiceProvider serviceProvider)
    {
        // Instantiate the runner
        var runner = serviceProvider.GetRequiredService<IMigrationRunner>();

        // Execute the migrations
        runner.MigrateUp();
    }

    internal sealed class MigrateUpDbCommand : Command<MigrateUpDbCommand.Settings>
    {
        public sealed class Settings : CommandSettings
        {
            [Description("Connection string of a DB to migrate up.")]
            [CommandArgument(0, "<connectionString>")]
            public string ConnectionString { get; init; } = default!;
        }

        public override int Execute([NotNull] CommandContext context, [NotNull] Settings settings)
        {
            Log.Logger = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture)
                .CreateBootstrapLogger();

            try
            {
                using var serviceProvider = CreateServices(settings.ConnectionString);
                using var scope = serviceProvider.CreateScope();

                UpdateDatabase(scope.ServiceProvider);

                return 0;
            }
            catch (Exception exception)
            {
                Log.Fatal(exception, "Application terminated unexpectedly.");
            }
            finally
            {
                Log.CloseAndFlush();
            }

            return 1;
        }
    }
}
