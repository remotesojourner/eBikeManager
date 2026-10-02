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
        services.AddSingleton<BridgeStateService>();
        services.AddSingleton<PkceLoginService>();
        services.AddSingleton<BoschConnectionService>();
        services.AddSingleton<GoogleHealthConnectionService>();
        services.AddSingleton<FitArchiveService>();
        services.AddSingleton<NotificationStateService>();

        services.AddScoped<SettingsService>();
        services.AddScoped<SignInService>();
        services.AddScoped<SecretStoreService>();
        services.AddScoped<BoschAccountService>();
        services.AddScoped<BikeService>();
        services.AddScoped<RideService>();
        services.AddScoped<StatisticsService>();
        services.AddScoped<BridgeService>();
        services.AddScoped<RideSyncService>();
        services.AddScoped<BikeDetailsSyncService>();
        services.AddScoped<SyncRunService>();
        services.AddScoped<GoogleHealthAccountService>();
        services.AddScoped<IntegrationSyncService>();
        services.AddScoped<IRideIntegration, GoogleHealthIntegration>();
        services.AddScoped<VersionService>();
        services.AddScoped<NotificationService>();
        services.AddNotifications();

        services.AddDbContext<EBikeManagerDbContext>((provider, options) =>
        {
            var databasePath = provider.GetRequiredService<IOptions<EBikeManagerOptions>>().Value.DatabasePath;
            options.UseSqlite($"Data Source={databasePath};Cache=Shared");
        });

        services.AddScoped<ISettingsRepository, SettingsRepository>();
        services.AddScoped<ISecretRepository, SecretRepository>();
        services.AddScoped<IBikeRepository, BikeRepository>();
        services.AddScoped<IBikePictureRepository, BikePictureRepository>();
        services.AddScoped<IBikeDocumentRepository, BikeDocumentRepository>();
        services.AddScoped<IRideRepository, RideRepository>();
        services.AddScoped<IRideExportRepository, RideExportRepository>();
        services.AddScoped<INotificationChannelRepository, NotificationChannelRepository>();

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

        services.AddHttpClient(GoogleHealthAuthService.HttpClientName, ConfigureBoschClient);
        services.AddSingleton<IGoogleHealthAuthService, GoogleHealthAuthService>();
        services.AddTransient<GoogleHealthAuthenticationHandler>();
        services.AddHttpClient<IGoogleHealthApiService, GoogleHealthApiService>(ConfigureBoschClient)
            .AddHttpMessageHandler<GoogleHealthAuthenticationHandler>()
            .AddStandardResilienceHandler(options =>
            {
                options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(30);
                options.CircuitBreaker.SamplingDuration = TimeSpan.FromMinutes(1);
                options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(90);
            });

        services.AddHttpClient(BoschApiService.MediaHttpClientName, http =>
        {
            ConfigureBoschClient(http);
            http.Timeout = TimeSpan.FromSeconds(60);
            http.MaxResponseContentBufferSize = ImageFormat.MaxBytes;
        });

        services.AddHttpClient(OidcDiscoveryService.HttpClientName, http =>
        {
            ConfigureBoschClient(http);
            http.Timeout = TimeSpan.FromSeconds(10);
        });
        services.AddSingleton<IOidcDiscoveryService, OidcDiscoveryService>();

        services.AddHttpClient(GitHubReleaseService.HttpClientName, http =>
        {
            ConfigureBoschClient(http);
            http.Timeout = TimeSpan.FromSeconds(5);
        });
        services.AddSingleton<IReleaseService, GitHubReleaseService>();

        services.AddHostedService<SyncSchedulerService>();
        services.AddHostedService<BridgeListenerService>();
        services.AddHostedService<NotificationTickerService>();
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
