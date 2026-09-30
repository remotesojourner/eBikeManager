using EBikeManager.Application.Configuration;
using EBikeManager.Application.Data;
using EBikeManager.Application.Models.Entities;
using EBikeManager.Application.Repositories;
using EBikeManager.Application.Services;
using EBikeManager.IntegrationTests.Fixtures;
using EBikeManager.TestSupport;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

namespace EBikeManager.IntegrationTests.Application.Services;

public sealed class RideSyncServiceTests : IDisposable
{
    private static readonly DateTime _now = new(2026, 9, 30, 12, 0, 0, DateTimeKind.Utc);

    private readonly TestDatabase _database = new();
    private readonly TemporaryFolder _folder = new();
    private readonly FakeBoschApi _bosch = new();
    private readonly FitArchiveService _archive;
    private readonly List<EBikeManagerDbContext> _contexts = [];

    public RideSyncServiceTests()
    {
        _archive = new FitArchiveService(Options.Create(new EBikeManagerOptions { DataDirectory = _folder.Path }));
    }

    public void Dispose()
    {
        foreach (var context in _contexts) context.Dispose();
        _database.Dispose();
        _folder.Dispose();
    }

    [Fact]
    public async Task TheFirstSyncBacksUpEveryRideOfTheChosenBikes()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await ChooseBikesAsync("bike-a");
        for (var index = 0; index < 35; index++) _bosch.AddRide($"a{index}", "bike-a", _now.AddDays(-30 - index));
        _bosch.AddRide("b0", "bike-b", _now.AddDays(-1));

        var result = await NewSync().RunAsync(cancellationToken);

        Assert.Equal((35, 35, 35), (result.RidesChecked, result.NewRides, result.FitFilesSaved));
        Assert.Empty(result.Problems);
        Assert.Equal([0, 1], _bosch.RequestedPages);
        Assert.DoesNotContain("b0", _bosch.DownloadedFits);

        var rides = await RidesAsync();
        Assert.Equal(35, rides.Count);
        Assert.All(rides, ride =>
        {
            Assert.Equal(410, ride.CaloriesKcal);
            Assert.True(File.Exists(_archive.FullPath(ride.FitPath!)));
            Assert.True(File.Exists(Path.ChangeExtension(_archive.FullPath(ride.FitPath!), ".json")));
        });
    }

    [Fact]
    public async Task LaterSyncsStopAtTheFirstPageWithNothingNew()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await ChooseBikesAsync("bike-a");
        for (var index = 0; index < 65; index++) _bosch.AddRide($"old{index}", "bike-a", _now.AddDays(-30 - index));
        await NewSync().RunAsync(cancellationToken);
        _bosch.RequestedPages.Clear();
        _bosch.DownloadedFits.Clear();
        _bosch.AddRide("new", "bike-a", _now.AddHours(-3));

        var result = await NewSync().RunAsync(cancellationToken);

        Assert.Equal([0, 1], _bosch.RequestedPages);
        Assert.Equal(["new"], _bosch.DownloadedFits);
        Assert.Equal(1, result.NewRides);
    }

    [Fact]
    public async Task ChangingTheBikesReadsTheWholeHistoryOnce()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await ChooseBikesAsync("bike-a");
        for (var index = 0; index < 65; index++) _bosch.AddRide($"old{index}", "bike-a", _now.AddDays(-30 - index));
        await NewSync().RunAsync(cancellationToken);
        await using (var db = NewContext())
        {
            await new SettingsRepository(db).SaveAsync(new Dictionary<string, string> { [SettingDefinitions.BoschFullScan] = "true" }, cancellationToken);
        }

        _bosch.RequestedPages.Clear();

        await NewSync().RunAsync(cancellationToken);

        Assert.Equal([0, 1, 2], _bosch.RequestedPages);
        await using var reader = NewContext();
        Assert.False((await new SettingsRepository(reader).GetAsync(cancellationToken)).Bosch.FullScanRequested);
    }

    [Fact]
    public async Task RidesWithoutAFitFileAreMarkedAndNotAskedForAgain()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await ChooseBikesAsync("bike-a");
        _bosch.AddRide("manual", "bike-a", _now.AddDays(-1), withFit: false);

        await NewSync().RunAsync(cancellationToken);
        await NewSync().RunAsync(cancellationToken);

        Assert.Equal(["manual"], _bosch.DownloadedFits);
        var ride = Assert.Single(await RidesAsync());
        Assert.True(ride.FitUnavailable);
        Assert.Null(ride.FitPath);
    }

    [Fact]
    public async Task DamagedFitFilesAreReportedAndRetriedOnTheNextSync()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await ChooseBikesAsync("bike-a");
        _bosch.AddRide("broken", "bike-a", _now.AddDays(-1));
        _bosch.FitFiles["broken"] = "not a fit file"u8.ToArray();

        var first = await NewSync().RunAsync(cancellationToken);
        Assert.Single(first.Problems);
        Assert.NotNull(Assert.Single(await RidesAsync()).FitError);

        _bosch.FitFiles["broken"] = TestFit.Create(_now.AddDays(-1), TimeSpan.FromHours(1));
        var second = await NewSync().RunAsync(cancellationToken);

        Assert.Empty(second.Problems);
        Assert.Equal(1, second.FitFilesSaved);
        var ride = Assert.Single(await RidesAsync());
        Assert.Null(ride.FitError);
        Assert.NotNull(ride.FitPath);
    }

    [Fact]
    public async Task RidesBoschIsStillProcessingAreDownloadedLater()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await ChooseBikesAsync("bike-a");
        var ride = _bosch.AddRide("fresh", "bike-a", _now.AddMinutes(-20));
        _bosch.Activities[0] = ride with { EndTime = null };

        await NewSync().RunAsync(cancellationToken);
        Assert.Empty(_bosch.DownloadedFits);

        _bosch.Activities[0] = ride;
        await NewSync().RunAsync(cancellationToken);
        Assert.Equal(["fresh"], _bosch.DownloadedFits);
    }

    [Fact]
    public async Task NothingHappensBeforeABikeIsChosen()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() => NewSync().RunAsync(TestContext.Current.CancellationToken));
    }

    private EBikeManagerDbContext NewContext()
    {
        var context = _database.NewContext();
        _contexts.Add(context);
        return context;
    }

    private RideSyncService NewSync()
    {
        var db = NewContext();
        return new RideSyncService(
            _bosch,
            new RideRepository(db),
            new BikeRepository(db),
            new SettingsRepository(db),
            _archive,
            new SyncStateService(new AppEventService(), TimeProvider.System),
            new FakeTimeProvider(new DateTimeOffset(_now)),
            NullLogger<RideSyncService>.Instance)
        {
            DownloadDelay = TimeSpan.Zero
        };
    }

    private async Task ChooseBikesAsync(params string[] bikeIds)
    {
        await using var db = _database.NewContext();
        await new BikeRepository(db).ReplaceAsync(bikeIds.Select(id => new Bike { Id = id, Name = id, AddedAt = _now }).ToList());
    }

    private async Task<List<Ride>> RidesAsync()
    {
        await using var db = _database.NewContext();
        return [.. await new RideRepository(db).GetListAsync()];
    }
}
