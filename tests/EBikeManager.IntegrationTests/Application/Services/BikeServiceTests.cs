using EBikeManager.Application.Enums;
using EBikeManager.Application.Models;
using EBikeManager.Application.Services;
using EBikeManager.IntegrationTests.Fixtures;
using EBikeManager.TestSupport;
using Microsoft.Extensions.DependencyInjection;

namespace EBikeManager.IntegrationTests.Application.Services;

public sealed class BikeServiceTests
{
    private const string Model = "TENWAYS (Performance Line)";

    [Fact]
    public async Task RenamingABikeRenamesItsRidesAndKeepsTheModelFromBosch()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var app = new SetUpApp();
        await app.InitializeAsync();
        using var scope = app.Services.CreateScope();
        var bikes = NewBikeService(scope.ServiceProvider);
        var rides = scope.ServiceProvider.GetRequiredService<RideService>();

        Assert.True((await bikes.SaveNamesAsync(new Dictionary<string, string> { [SampleData.BikeId] = "  Commuter " }, cancellationToken)).Succeeded);
        Assert.True((await bikes.SaveSelectionAsync([new BoschBikeInfo(SampleData.BikeId, Model)], cancellationToken)).Succeeded);

        var bike = Assert.Single(await bikes.GetBikesAsync(cancellationToken));
        Assert.Equal(("Commuter", Model), (bike.Name, bike.Model));
        Assert.All(await rides.GetRidesAsync(cancellationToken: cancellationToken), ride => Assert.Equal(("Commuter", Model), (ride.BikeName, ride.BikeModel)));
    }

    [Fact]
    public async Task RidesKeepTheirBikesNameAndModelOnceTheBikeIsUnticked()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var app = new SetUpApp();
        await app.InitializeAsync();
        using var scope = app.Services.CreateScope();
        var bikes = NewBikeService(scope.ServiceProvider);
        var rides = scope.ServiceProvider.GetRequiredService<RideService>();
        await bikes.SaveNamesAsync(new Dictionary<string, string> { [SampleData.BikeId] = "Commuter" }, cancellationToken);

        Assert.True((await bikes.SaveSelectionAsync([new BoschBikeInfo(SampleData.OtherBikeId, "Cube (Performance Line CX)")], cancellationToken)).Succeeded);

        Assert.Equal([SampleData.OtherBikeId], (await bikes.GetBikesAsync(cancellationToken)).Select(bike => bike.Id));
        var detail = (await rides.GetDetailAsync("ride-00", cancellationToken)).Value!;
        Assert.Equal(("Commuter", Model), (detail.BikeName, detail.BikeModel));
    }

    [Fact]
    public async Task EveryBikeNeedsAShortName()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var app = new SetUpApp();
        await app.InitializeAsync();
        using var scope = app.Services.CreateScope();
        var bikes = NewBikeService(scope.ServiceProvider);

        var blank = await bikes.SaveNamesAsync(new Dictionary<string, string> { [SampleData.BikeId] = " " }, cancellationToken);
        var tooLong = await bikes.SaveNamesAsync(new Dictionary<string, string> { [SampleData.BikeId] = new string('x', BikeService.MaxNameLength + 1) }, cancellationToken);
        var unknown = await bikes.SaveNamesAsync(new Dictionary<string, string> { ["no-such-bike"] = "Commuter" }, cancellationToken);

        Assert.Equal((OperationOutcome.Invalid, OperationOutcome.Invalid, OperationOutcome.NotFound), (blank.Outcome, tooLong.Outcome, unknown.Outcome));
        Assert.Equal(Model, Assert.Single(await bikes.GetBikesAsync(cancellationToken)).Name);
    }

    private static BikeService NewBikeService(IServiceProvider services) =>
        ActivatorUtilities.CreateInstance<BikeService>(services, FixedAccess.Full, ActivatorUtilities.CreateInstance<SettingsService>(services, FixedAccess.Full));
}
