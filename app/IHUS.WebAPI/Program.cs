using IHUS.Database;
using IHUS.Database.Repositories;
using IHUS.Domain.Services.Generation.Implementations;
using IHUS.Domain.Services.Generation.Interfaces;
using IHUS.Domain.Services.Repositories;
using Microsoft.EntityFrameworkCore;

namespace IHUS.WebAPI
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var app = BuildApp(args);

            ConfigureMiddlewares(app);
            await Initialize(app);

            await app.RunAsync();
        }

        private static async Task Initialize(WebApplication app)
        {
            using var scope = app.Services.CreateScope();

            var dbContext = scope.ServiceProvider.GetRequiredService<IHUSDbContext>();

            await dbContext.Database.MigrateAsync();
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
                .Services.AddDbContextPool<IHUSDbContext>(options =>
                    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

            builder.Services
                .AddSingleton<IHashProvider, Sha256HashProvider>()
                .AddSingleton<ISaltProvider, RngSaltProvider>()
                .AddScoped<IShortenedUrlRepository, ShortenedUrlRepository>()
                .AddScoped<IShortenedUrlGenerator, HashBasedUrlShortener>();

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
            app.UseHttpsRedirection();
            app.UseAuthorization();
            app.MapControllers();
        }
    }
}
