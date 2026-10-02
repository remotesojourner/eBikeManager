using EBikeManager.Application.Repositories.Interfaces;
using EBikeManager.Application.Services;
using EBikeManager.Application.Services.Interfaces;
using EBikeManager.IntegrationTests.Fixtures;
using EBikeManager.Web.Services;
using Microsoft.Extensions.DependencyInjection;

namespace EBikeManager.IntegrationTests.Web.Installers;

public sealed class ServiceRegistrationTests : IClassFixture<NotSetUpApp>
{
    private static readonly Type[] _services =
    [
        typeof(IAppEventService),
        typeof(ICurrentAccessService),
        typeof(ISignInStateService),
        typeof(ISecretProtectionService),
        typeof(IBoschApiService),
        typeof(IBoschAuthService),
        typeof(SettingsService),
        typeof(SignInService),
        typeof(SecretStoreService),
        typeof(BoschAccountService),
        typeof(BoschConnectionService),
        typeof(BikeService),
        typeof(RideService),
        typeof(RideSyncService),
        typeof(BikeDetailsSyncService),
        typeof(IBikePictureRepository),
        typeof(IBikeDocumentRepository),
        typeof(IRideExportRepository),
        typeof(IGoogleHealthApiService),
        typeof(IGoogleHealthAuthService),
        typeof(IRideIntegration),
        typeof(GoogleHealthConnectionService),
        typeof(GoogleHealthAccountService),
        typeof(IntegrationSyncService),
        typeof(SyncRunService),
        typeof(SyncStateService),
        typeof(PkceLoginService),
        typeof(FitArchiveService),
        typeof(AuthSettingsService),
        typeof(IOidcDiscoveryService),
        typeof(IReleaseService),
        typeof(VersionService),
        typeof(NotificationService),
        typeof(NotificationStateService),
        typeof(INotificationDispatchService),
        typeof(INotificationChannelRepository),
        typeof(CircuitAccessService),
        typeof(BrowserInteropService),
        typeof(PreferencesService),
        typeof(SettingsStateService),
        typeof(SyncStatusStateService),
        typeof(LiveUpdatesService)
    ];

    private readonly NotSetUpApp _app;

    public ServiceRegistrationTests(NotSetUpApp app)
    {
        _app = app;
    }

    [Fact]
    public void EveryServiceTheAppUsesCanBeResolved()
    {
        using var scope = _app.Services.CreateScope();

        Assert.All(_services, service => Assert.NotNull(scope.ServiceProvider.GetRequiredService(service)));
    }
}
