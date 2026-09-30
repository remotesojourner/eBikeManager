using EBikeManager.Application.Configuration;
using EBikeManager.Application.Models;
using EBikeManager.Application.Models.Entities;
using EBikeManager.Application.Repositories.Interfaces;
using EBikeManager.Application.Services;
using EBikeManager.TestSupport;
using Microsoft.Extensions.DependencyInjection;

namespace EBikeManager.IntegrationTests.Fixtures;

internal static class SampleData
{
    public const string BikeId = "bike-1";
    public const string OtherBikeId = "bike-2";

    public static void AddBikesAndRides(FakeBoschApi bosch, int rides = 30)
    {
        bosch.Bikes.Add(new BoschBikeInfo(BikeId, "TENWAYS (Performance Line)"));
        bosch.Bikes.Add(new BoschBikeInfo(OtherBikeId, "Cube (Performance Line CX)"));
        var now = DateTime.UtcNow;
        for (var index = 0; index < rides; index++)
        {
            bosch.AddRide($"ride-{index:00}", BikeId, now.AddDays(-index * 2).AddHours(-3), withFit: index != 4, title: index % 3 == 0 ? "London roundtrip" : null);
        }

        bosch.FitFiles["ride-05"] = "broken"u8.ToArray();
    }

    public static async Task CompleteSetupAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        await services.GetRequiredService<SecretStoreService>().SetAsync(SecretStoreService.BoschRefreshToken, "refresh", cancellationToken);
        await services.GetRequiredService<IBikeRepository>().ReplaceAsync([new Bike { Id = BikeId, Name = "TENWAYS (Performance Line)", AddedAt = DateTime.UtcNow }], cancellationToken);
        await services.GetRequiredService<ISettingsRepository>().SaveAsync(new Dictionary<string, string>
        {
            [SettingDefinitions.SetupCompleted] = "true",
            [SettingDefinitions.BoschAccount] = FakeBoschAuth.Account
        }, cancellationToken);
    }

    public static async Task SyncAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        var sync = services.GetRequiredService<RideSyncService>();
        sync.DownloadDelay = TimeSpan.Zero;
        await sync.RunAsync(cancellationToken);
    }
}
