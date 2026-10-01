using EBikeManager.Application.BackgroundServices;
using EBikeManager.Application.Enums;
using EBikeManager.Application.Models;
using EBikeManager.Application.Models.Entities;
using EBikeManager.Application.Repositories.Interfaces;
using EBikeManager.Application.Resources;
using EBikeManager.Application.Services;
using EBikeManager.IntegrationTests.Fixtures;
using EBikeManager.TestSupport;
using Microsoft.Extensions.DependencyInjection;

namespace EBikeManager.IntegrationTests.Application.Services;

public sealed class BridgeListenerServiceTests
{
    private static readonly TimeSpan _patience = TimeSpan.FromSeconds(15);

    [Fact]
    public async Task ThePlainBridgeShowsLiveValuesAndKeepsTheBatteryAndOdometer()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var app = await StartAppAsync(cancellationToken);
        await using var bridge = new FakeEsphomeBridge();
        await bridge.SetAsync(FakeEsphomeBridge.Battery, 50f);
        await bridge.SetAsync(FakeEsphomeBridge.Odometer, 37.3f);
        await bridge.SetAsync(FakeEsphomeBridge.Speed, 12.5f);
        await bridge.SetAsync(FakeEsphomeBridge.SystemLocked, false);
        await bridge.SetAsync(FakeEsphomeBridge.Connected, true);
        await SaveAsync(app, new BridgeRequest(bridge.Address, null, null, null, false), cancellationToken);

        await using var listener = await StartListenerAsync(app, cancellationToken);
        var snapshot = await WaitForAsync(app, current => current.LiveFor(SampleData.BikeId)?.SpeedKmh == 12.5);

        Assert.Equal(BridgeConnectionState.Connected, snapshot.Status.State);
        Assert.Equal(FakeEsphomeBridge.FriendlyName, snapshot.Status.Device?.FriendlyName);
        Assert.False(snapshot.Status.IsDual);
        var live = snapshot.LiveFor(SampleData.BikeId)!;
        Assert.Equal(50, live.BatteryPercent);
        Assert.True(live.Locked);
        var bike = await BikeAsync(app, SampleData.BikeId, cancellationToken);
        Assert.Equal(50, bike.BridgeBatteryPercent);
        Assert.Equal(37.3, bike.BridgeOdometerKm!.Value, 0.01);

        await bridge.SetAsync(FakeEsphomeBridge.Odometer, 38.5f);
        await bridge.SetAsync(FakeEsphomeBridge.Connected, false);
        Assert.Null((await WaitForAsync(app, current => current.Slot(1) is { Connected: false, OdometerKm: > 38 })).LiveFor(SampleData.BikeId));
        await WaitUntilAsync(async () => (await BikeAsync(app, SampleData.BikeId, cancellationToken)).BridgeOdometerKm > 38);
    }

    [Fact]
    public async Task AnEncryptedBridgeNeedsItsKey()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var key = FakeEsphomeBridge.NewKey();
        await using var app = await StartAppAsync(cancellationToken);
        await using var bridge = new FakeEsphomeBridge(Convert.FromBase64String(key));
        await bridge.SetAsync(FakeEsphomeBridge.Battery, 64f);
        await using var listener = await StartListenerAsync(app, cancellationToken);

        await SaveAsync(app, new BridgeRequest(bridge.Address, null, null, null, false), cancellationToken);
        await WaitForAsync(app, current => current.Status.Problem == ApplicationStrings.BridgeNeedsKey);

        await SaveAsync(app, new BridgeRequest(bridge.Address, null, null, FakeEsphomeBridge.NewKey(), false), cancellationToken);
        await WaitForAsync(app, current => current.Status.Problem == ApplicationStrings.BridgeWrongKey);

        await SaveAsync(app, new BridgeRequest(bridge.Address, null, null, key, false), cancellationToken);
        var snapshot = await WaitForAsync(app, current => current.Slot(1)?.BatteryPercent == 64);
        Assert.Equal(BridgeConnectionState.Connected, snapshot.Status.State);
    }

    [Fact]
    public async Task APlainBridgeTellsYouToRemoveTheKey()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var app = await StartAppAsync(cancellationToken);
        await using var bridge = new FakeEsphomeBridge();
        await SaveAsync(app, new BridgeRequest(bridge.Address, null, null, FakeEsphomeBridge.NewKey(), false), cancellationToken);

        await using var listener = await StartListenerAsync(app, cancellationToken);

        Assert.Equal(ApplicationStrings.BridgeNotEncrypted, (await WaitForFailureAsync(app)).Status.Problem);
    }

    [Fact]
    public async Task ADualBridgeFeedsEachOfItsBikesToTheChosenBike()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var app = await StartAppAsync(cancellationToken);
        using (var scope = app.Services.CreateScope())
        {
            await scope.ServiceProvider.GetRequiredService<IBikeRepository>().ReplaceAsync(
            [
                new Bike { Id = SampleData.BikeId, Name = "TENWAYS (Performance Line)", Model = "TENWAYS (Performance Line)", AddedAt = DateTime.UtcNow },
                new Bike { Id = SampleData.OtherBikeId, Name = "Cube (Performance Line CX)", Model = "Cube (Performance Line CX)", AddedAt = DateTime.UtcNow }
            ], cancellationToken);
        }

        await using var bridge = new FakeEsphomeBridge(dual: true);
        await bridge.SetAsync(FakeEsphomeBridge.Battery, 80f, slot: 1);
        await bridge.SetAsync(FakeEsphomeBridge.Battery, 40f, slot: 2);
        await SaveAsync(app, new BridgeRequest(bridge.Address, SampleData.OtherBikeId, SampleData.BikeId, null, false), cancellationToken);

        await using var listener = await StartListenerAsync(app, cancellationToken);
        var snapshot = await WaitForAsync(app, current => current.Slot(1)?.BatteryPercent == 80 && current.Slot(2)?.BatteryPercent == 40);

        Assert.True(snapshot.Status.IsDual);
        Assert.Equal((SampleData.OtherBikeId, SampleData.BikeId), (snapshot.Slot(1)!.BikeId, snapshot.Slot(2)!.BikeId));
        await WaitUntilAsync(async () => (await BikeAsync(app, SampleData.BikeId, cancellationToken)).BridgeBatteryPercent == 40);
        Assert.Equal(80, (await BikeAsync(app, SampleData.OtherBikeId, cancellationToken)).BridgeBatteryPercent);
    }

    [Fact]
    public async Task ABridgeThatCantBeReachedIsReported()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var app = await StartAppAsync(cancellationToken);
        await SaveAsync(app, new BridgeRequest("127.0.0.1:1", null, null, null, false), cancellationToken);

        await using var listener = await StartListenerAsync(app, cancellationToken);

        Assert.StartsWith("Can't reach the bridge at 127.0.0.1:1", (await WaitForFailureAsync(app)).Status.Problem, StringComparison.Ordinal);
    }

    [Fact]
    public async Task WithoutAnAddressTheBridgeIsOffAndALostConnectionIsRetried()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var app = await StartAppAsync(cancellationToken);
        await using var bridge = new FakeEsphomeBridge();
        await using var listener = await StartListenerAsync(app, cancellationToken);
        Assert.Equal(BridgeConnectionState.Off, (await WaitForAsync(app, current => current.Status.State == BridgeConnectionState.Off)).Status.State);

        await SaveAsync(app, new BridgeRequest(bridge.Address, null, null, null, false), cancellationToken);
        await WaitForAsync(app, current => current.Status.State == BridgeConnectionState.Connected);
        await bridge.DisconnectEveryoneAsync();

        await WaitUntilAsync(() => Task.FromResult(bridge.Connections == 2));
        await WaitForAsync(app, current => current.Status.State == BridgeConnectionState.Connected);
    }

    private static async Task<SetUpApp> StartAppAsync(CancellationToken cancellationToken)
    {
        var app = new SetUpApp();
        await app.InitializeAsync();
        cancellationToken.ThrowIfCancellationRequested();
        return app;
    }

    private static async Task SaveAsync(SetUpApp app, BridgeRequest request, CancellationToken cancellationToken)
    {
        using var scope = app.Services.CreateScope();
        var settings = ActivatorUtilities.CreateInstance<SettingsService>(scope.ServiceProvider, FixedAccess.Full);
        var result = await ActivatorUtilities.CreateInstance<BridgeService>(scope.ServiceProvider, settings, FixedAccess.Full).SaveAsync(request, cancellationToken);
        Assert.True(result.Succeeded, result.Message);
    }

    private static async Task<RunningListener> StartListenerAsync(SetUpApp app, CancellationToken cancellationToken)
    {
        var listener = ActivatorUtilities.CreateInstance<BridgeListenerService>(app.Services);
        listener.FirstRetry = TimeSpan.FromMilliseconds(200);
        await listener.StartAsync(cancellationToken);
        return new RunningListener(listener);
    }

    private static async Task<Bike> BikeAsync(SetUpApp app, string bikeId, CancellationToken cancellationToken)
    {
        using var scope = app.Services.CreateScope();
        return (await scope.ServiceProvider.GetRequiredService<IBikeRepository>().GetAllAsync(cancellationToken)).Single(bike => bike.Id == bikeId);
    }

    private static Task<BridgeSnapshot> WaitForFailureAsync(SetUpApp app) =>
        WaitForAsync(app, current => current.Status.State == BridgeConnectionState.Failed);

    private static async Task<BridgeSnapshot> WaitForAsync(SetUpApp app, Func<BridgeSnapshot, bool> condition)
    {
        var state = app.Services.GetRequiredService<BridgeStateService>();
        await WaitUntilAsync(() => Task.FromResult(condition(state.Snapshot)));
        return state.Snapshot;
    }

    private static async Task WaitUntilAsync(Func<Task<bool>> condition)
    {
        var deadline = DateTime.UtcNow + _patience;
        while (!await condition())
        {
            Assert.True(DateTime.UtcNow < deadline, "The bridge didn't reach the expected state in time.");
            await Task.Delay(50, TestContext.Current.CancellationToken);
        }
    }

    private sealed class RunningListener(BridgeListenerService listener) : IAsyncDisposable
    {
        public async ValueTask DisposeAsync()
        {
            await listener.StopAsync(CancellationToken.None);
            listener.Dispose();
        }
    }
}
