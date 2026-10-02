using EBikeManager.Application.Enums;
using EBikeManager.Application.Services;
using EBikeManager.IntegrationTests.Fixtures;
using Microsoft.Extensions.DependencyInjection;

namespace EBikeManager.IntegrationTests.Application.Services;

public sealed class RideServiceTests : IClassFixture<SetUpApp>
{
    private readonly SetUpApp _app;

    public RideServiceTests(SetUpApp app)
    {
        _app = app;
    }

    [Fact]
    public async Task ARideWithABackupHasItsRouteChartsAndSplits()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var scope = _app.Services.CreateScope();

        var result = await scope.ServiceProvider.GetRequiredService<RideService>().GetDetailAsync("ride-00", UnitSystem.Metric, cancellationToken);

        var ride = result.Value!;
        Assert.Equal("TENWAYS (Performance Line)", ride.BikeName);
        Assert.Equal(["TURBO", "ECO"], ride.AssistModes.Select(mode => mode.Name));
        var track = Assert.IsType<EBikeManager.Application.Models.Dtos.RideTrackDto>(ride.Track);
        Assert.Null(ride.TrackProblem);
        Assert.Equal(721, track.Route.Count);
        Assert.True(track.Series is { HasElevation: true, HasSpeed: true, HasCadence: true, HasPower: true, HasHeartRate: false });
        Assert.Equal(13, track.Splits.Count);
        Assert.Equal(12.5, track.Series.DistanceKm[^1]);
    }

    [Fact]
    public async Task ImperialRidesAreSplitByTheMile()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var scope = _app.Services.CreateScope();

        var result = await scope.ServiceProvider.GetRequiredService<RideService>().GetDetailAsync("ride-00", UnitSystem.Imperial, cancellationToken);

        var splits = result.Value!.Track!.Splits;
        Assert.Equal(8, splits.Count);
        Assert.All(splits.SkipLast(1), split => Assert.InRange(split.DistanceKm, 1.6, 1.66));
        Assert.Equal(12.5, splits.Sum(split => split.DistanceKm), 1);
    }

    [Fact]
    public async Task RidesWithoutABackupSayWhy()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var scope = _app.Services.CreateScope();
        var rides = scope.ServiceProvider.GetRequiredService<RideService>();

        var unavailable = (await rides.GetDetailAsync("ride-04", UnitSystem.Metric, cancellationToken)).Value!;
        var broken = (await rides.GetDetailAsync("ride-05", UnitSystem.Metric, cancellationToken)).Value!;
        var missing = await rides.GetDetailAsync("no-such-ride", UnitSystem.Metric, cancellationToken);

        Assert.Null(unavailable.Track);
        Assert.Contains("Bosch has no FIT file", unavailable.TrackProblem, StringComparison.Ordinal);
        Assert.Contains("hasn't been backed up yet", broken.TrackProblem, StringComparison.Ordinal);
        Assert.Equal(OperationOutcome.NotFound, missing.Outcome);
    }

    [Fact]
    public async Task TheMapPreviewShowsTheLatestRideWithARoute()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var scope = _app.Services.CreateScope();

        var route = await scope.ServiceProvider.GetRequiredService<RideService>().GetLatestRouteAsync(cancellationToken);

        Assert.Equal(721, route.Count);
    }
}
