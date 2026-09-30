using EBikeManager.Application.BackgroundServices;
using EBikeManager.Application.Configuration;
using EBikeManager.Application.Data;
using EBikeManager.Application.Handlers;
using EBikeManager.Application.Repositories;
using EBikeManager.Application.Repositories.Interfaces;
using EBikeManager.Application.Services;
using EBikeManager.Application.Services.Interfaces;
using EBikeManager.Application.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace EBikeManager.Application.Installers;

public static class ApplicationInstaller
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton<IAppEventService, AppEventService>();
        services.AddSingleton<SyncStateService>();
        services.AddSingleton<PkceLoginService>();
        services.AddSingleton<BoschConnectionService>();
        services.AddSingleton<FitArchiveService>();

        services.AddScoped<SettingsService>();
        services.AddScoped<SignInService>();
        services.AddScoped<SecretStoreService>();
        services.AddScoped<BoschAccountService>();
        services.AddScoped<BikeService>();
        services.AddScoped<RideService>();
        services.AddScoped<RideSyncService>();
        services.AddScoped<SyncRunService>();

        services.AddDbContext<EBikeManagerDbContext>((provider, options) =>
        {
            var databasePath = provider.GetRequiredService<IOptions<EBikeManagerOptions>>().Value.DatabasePath;
            options.UseSqlite($"Data Source={databasePath};Cache=Shared");
        });

        services.AddScoped<ISettingsRepository, SettingsRepository>();
        services.AddScoped<ISecretRepository, SecretRepository>();
        services.AddScoped<IBikeRepository, BikeRepository>();
        services.AddScoped<IRideRepository, RideRepository>();

        services.TryAddSingleton(TimeProvider.System);

        services.AddHttpClient(BoschAuthService.HttpClientName, ConfigureBoschClient);
        services.AddSingleton<IBoschAuthService, BoschAuthService>();
        services.AddTransient<BoschAuthenticationHandler>();
        services.AddHttpClient<IBoschApiService, BoschApiService>(ConfigureBoschClient)
            .AddHttpMessageHandler<BoschAuthenticationHandler>()
            .AddStandardResilienceHandler(options =>
            {
                options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(30);
                options.CircuitBreaker.SamplingDuration = TimeSpan.FromMinutes(1);
                options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(90);
            });

        services.AddHostedService<SyncSchedulerService>();
        return services;
    }

    public static async Task InitializeDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EBikeManagerDbContext>();
        await db.Database.MigrateAsync(cancellationToken);
        await db.Database.ExecuteSqlRawAsync("PRAGMA journal_mode = WAL;", cancellationToken);
        await scope.ServiceProvider.GetRequiredService<ISettingsRepository>().InsertDefaultsAsync(cancellationToken);
    }

    private static void ConfigureBoschClient(HttpClient http) =>
        http.DefaultRequestHeaders.UserAgent.ParseAdd($"eBikeManager/{ProjectInfo.Version}");
}
