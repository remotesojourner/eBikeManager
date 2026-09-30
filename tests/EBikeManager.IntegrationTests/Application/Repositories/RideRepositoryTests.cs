using EBikeManager.Application.Models.Entities;
using EBikeManager.Application.Repositories;
using EBikeManager.IntegrationTests.Fixtures;

namespace EBikeManager.IntegrationTests.Application.Repositories;

public sealed class RideRepositoryTests : IDisposable
{
    private static readonly DateTime _now = new(2026, 9, 30, 12, 0, 0, DateTimeKind.Utc);

    private readonly TestDatabase _database = new();

    public void Dispose() => _database.Dispose();

    [Fact]
    public async Task RidesComeNewestFirstAndTotalsCoverThePeriodAsked()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using (var db = _database.NewContext())
        {
            var repository = new RideRepository(db);
            repository.Add(NewRide("old", _now.AddDays(-40), 10_000, 30));
            repository.Add(NewRide("recent", _now.AddDays(-2), 20_000, 45.5));
            repository.Add(NewRide("today", _now.AddHours(-1), 5_000, null));
            await repository.SaveChangesAsync(cancellationToken);
        }

        await using var reader = _database.NewContext();
        var rides = new RideRepository(reader);

        Assert.Equal(["today", "recent", "old"], (await rides.GetListAsync(cancellationToken: cancellationToken)).Select(ride => ride.Id));
        Assert.Equal(["today", "recent"], (await rides.GetListAsync(2, cancellationToken)).Select(ride => ride.Id));

        var month = await rides.TotalsSinceAsync(_now.AddDays(-30), cancellationToken);
        Assert.Equal(2, month.Rides);
        Assert.Equal(25_000, month.DistanceMeters);
        Assert.Equal(45.5, month.CaloriesKcal);

        var all = await rides.TotalsSinceAsync(null, cancellationToken);
        Assert.Equal(3, all.Rides);
        Assert.Equal(0, (await rides.TotalsSinceAsync(_now.AddDays(1), cancellationToken)).Rides);
        Assert.Equal(DateTimeKind.Utc, (await rides.FindAsync("old", cancellationToken))!.StartTime.Kind);
    }

    private static Ride NewRide(string id, DateTime start, int meters, double? kcal) =>
        new() { Id = id, BikeId = "bike", StartTime = start, EndTime = start.AddHours(1), DistanceMeters = meters, CaloriesKcal = kcal, MovingSeconds = 1800 };
}
