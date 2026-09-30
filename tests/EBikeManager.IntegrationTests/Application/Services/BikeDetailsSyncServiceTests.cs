using EBikeManager.Application.Models.Entities;
using EBikeManager.Application.Repositories;
using EBikeManager.Application.Services;
using EBikeManager.IntegrationTests.Fixtures;
using EBikeManager.TestSupport;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;

namespace EBikeManager.IntegrationTests.Application.Services;

public sealed class BikeDetailsSyncServiceTests : IDisposable
{
    private const string OtherPictureUrl = "https://cdn.example.test/bikes/cube.jpg";

    private static readonly DateTime _now = new(2026, 9, 30, 12, 0, 0, DateTimeKind.Utc);
    private static readonly byte[] _jpeg = [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10];

    private readonly TestDatabase _database = new();
    private readonly FakeBoschApi _bosch = new();

    public void Dispose() => _database.Dispose();

    [Fact]
    public async Task EveryChosenBikeGetsAFreshSnapshot()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        _bosch.AddBike("bike-1", "TENWAYS (Performance Line)");
        _bosch.StatesOfCharge["bike-1"] = BoschSamples.StateOfCharge;
        _bosch.HasFlowPlus = true;
        await ChooseAsync("bike-1");

        var problems = await RefreshAsync(cancellationToken);

        Assert.Empty(problems);
        var bike = Assert.Single(await BikesAsync());
        Assert.Contains("PowerTube 540", bike.ProfileJson, StringComparison.Ordinal);
        Assert.Equal(BoschSamples.StateOfCharge, bike.StateOfChargeJson);
        Assert.Contains("WTEN123456789", bike.PassJson, StringComparison.Ordinal);
        Assert.Null(bike.LocationJson);
        Assert.True(bike.HasFlowPlus);
        Assert.Equal(_now, bike.DetailsUpdatedAt);
    }

    [Fact]
    public async Task ABikeBoschCannotDescribeIsReportedAndTheOthersStillRefresh()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        _bosch.AddBike("bike-1", "TENWAYS (Performance Line)");
        _bosch.AddBike("bike-2", "Cube (Performance Line CX)");
        _bosch.FailingBikes.Add("bike-2");
        await ChooseAsync("bike-1", "bike-2");

        var problems = await RefreshAsync(cancellationToken);

        Assert.Contains("bike-2", Assert.Single(problems), StringComparison.Ordinal);
        var bikes = await BikesAsync();
        Assert.NotNull(bikes.Single(bike => bike.Id == "bike-1").DetailsUpdatedAt);
        Assert.Null(bikes.Single(bike => bike.Id == "bike-2").DetailsUpdatedAt);
    }

    [Fact]
    public async Task ThePictureIsDownloadedOnceAndKeptLocally()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        _bosch.AddBike("bike-1", "TENWAYS (Performance Line)");
        await ChooseAsync("bike-1");

        await RefreshAsync(cancellationToken);
        await RefreshAsync(cancellationToken);

        var picture = await PictureAsync("bike-1");
        Assert.NotNull(picture);
        Assert.Equal(("image/png", BoschSamples.PictureUrl, _now), (picture.ContentType, picture.SourceUrl, picture.SavedAt));
        Assert.Equal(BoschSamples.Picture, picture.Content);
        Assert.Single(_bosch.DownloadedPictures);
    }

    [Fact]
    public async Task ANewPictureAddressReplacesTheStoredPicture()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        _bosch.AddBike("bike-1", "TENWAYS (Performance Line)");
        await ChooseAsync("bike-1");
        await RefreshAsync(cancellationToken);

        _bosch.Profiles["bike-1"] = BoschSamples.Profile("bike-1").Replace(BoschSamples.PictureUrl, OtherPictureUrl, StringComparison.Ordinal);
        _bosch.Pictures[OtherPictureUrl] = _jpeg;
        await RefreshAsync(cancellationToken);

        var picture = await PictureAsync("bike-1");
        Assert.Equal(("image/jpeg", OtherPictureUrl), (picture?.ContentType, picture?.SourceUrl));
        Assert.Equal(2, _bosch.DownloadedPictures.Count);
    }

    [Fact]
    public async Task APictureThatIsNotAnImageOrCannotBeDownloadedIsSkipped()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        _bosch.AddBike("bike-1", "TENWAYS (Performance Line)");
        _bosch.AddBike("bike-2", "Cube (Performance Line CX)");
        _bosch.Pictures[BoschSamples.PictureUrl] = "<svg xmlns='http://www.w3.org/2000/svg'/>"u8.ToArray();
        _bosch.Profiles["bike-2"] = BoschSamples.Profile("bike-2").Replace(BoschSamples.PictureUrl, OtherPictureUrl, StringComparison.Ordinal);
        _bosch.FailingPictures.Add(OtherPictureUrl);
        await ChooseAsync("bike-1", "bike-2");

        var problems = await RefreshAsync(cancellationToken);

        Assert.Empty(problems);
        Assert.Null(await PictureAsync("bike-1"));
        Assert.Null(await PictureAsync("bike-2"));
        Assert.All(await BikesAsync(), bike => Assert.Equal(_now, bike.DetailsUpdatedAt));
    }

    [Fact]
    public async Task ThePictureIsRemovedWithItsBike()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        _bosch.AddBike("bike-1", "TENWAYS (Performance Line)");
        await ChooseAsync("bike-1");
        await RefreshAsync(cancellationToken);

        await ChooseAsync("bike-2");

        Assert.Null(await PictureAsync("bike-1"));
    }

    private async Task<IReadOnlyList<string>> RefreshAsync(CancellationToken cancellationToken)
    {
        await using var db = _database.NewContext();
        var service = new BikeDetailsSyncService(
            _bosch,
            new BikeRepository(db),
            new BikePictureRepository(db),
            new FakeTimeProvider(new DateTimeOffset(_now)),
            NullLogger<BikeDetailsSyncService>.Instance);
        return await service.RefreshAsync(cancellationToken);
    }

    private async Task<BikePicture?> PictureAsync(string bikeId)
    {
        await using var db = _database.NewContext();
        return await new BikePictureRepository(db).FindAsync(bikeId);
    }

    private async Task ChooseAsync(params string[] bikeIds)
    {
        await using var db = _database.NewContext();
        await new BikeRepository(db).ReplaceAsync(bikeIds.Select(id => new Bike { Id = id, Name = id, AddedAt = _now }).ToList());
    }

    private async Task<IReadOnlyList<Bike>> BikesAsync()
    {
        await using var db = _database.NewContext();
        return await new BikeRepository(db).GetAllAsync();
    }
}
