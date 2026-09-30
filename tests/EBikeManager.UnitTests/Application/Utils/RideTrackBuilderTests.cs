using EBikeManager.Application.Models;
using EBikeManager.Application.Utils;

namespace EBikeManager.UnitTests.Application.Utils;

public class RideTrackBuilderTests
{
    private static readonly DateTime _start = new(2026, 9, 6, 16, 12, 0, DateTimeKind.Utc);

    [Fact]
    public void RecordsOfTheSameSecondAreMergedAndValuesCarryForwardUntilTheyChange()
    {
        TrackRecord[] records =
        [
            new(_start, Latitude: 55.93, Longitude: -3.12),
            new(_start, AltitudeMeters: 50, SpeedMetresPerSecond: 5, Cadence: 60, PowerWatts: 100, DistanceMeters: 0),
            new(_start.AddSeconds(1), DistanceMeters: 5),
            new(_start.AddSeconds(2), Latitude: 55.9301, Longitude: -3.1201),
            new(_start.AddSeconds(2), SpeedMetresPerSecond: 6, DistanceMeters: 10)
        ];

        var track = RideTrackBuilder.Build(records);

        Assert.Equal([[-3.12, 55.93], [-3.1201, 55.9301]], track.Route);
        Assert.Equal([0, 0.005, 0.01], track.Series.DistanceKm);
        Assert.Equal([18, 18, 21.6], track.Series.Speed);
        Assert.Equal([50, 50, 50], track.Series.Elevation);
        Assert.Equal([100, 100, 100], track.Series.Power);
        Assert.Equal([55.93, 55.93, 55.9301], track.Series.Latitude);
        Assert.False(track.Series.HasHeartRate);
    }

    [Fact]
    public void LongRidesAreThinnedForTheMapAndCharts()
    {
        var records = Enumerable.Range(0, 20_000)
            .Select(second => new TrackRecord(_start.AddSeconds(second), 55 + second * 0.0001, -3, 50, 5, 70, 150, null, second * 5.0))
            .ToList();

        var track = RideTrackBuilder.Build(records);

        Assert.Equal(RideTrackBuilder.MaxRoutePoints, track.Route.Count);
        Assert.Equal([-3, 55], track.Route[0]);
        Assert.Equal([-3, 56.9999], track.Route[^1]);
        Assert.InRange(track.Series.DistanceKm.Count, RideTrackBuilder.MaxSamples - 1, RideTrackBuilder.MaxSamples + 1);
        Assert.Equal(99.995, track.Series.DistanceKm[^1]);
    }

    [Fact]
    public void SplitsAreTimedWhileMovingAndAverageOnlyThePedalling()
    {
        var records = new List<TrackRecord>();
        var time = _start;
        for (var metres = 0; metres <= 2_500; metres += 50)
        {
            var record = new TrackRecord(time, AltitudeMeters: 40 + metres / 100.0, Cadence: metres % 100 == 0 ? 80 : 0, PowerWatts: metres % 100 == 0 ? 200 : 0, DistanceMeters: metres);
            records.Add(record);
            if (metres == 1_200)
            {
                time = time.AddSeconds(300);
                records.Add(record with { Time = time });
            }

            time = time.AddSeconds(10);
        }

        var splits = RideTrackBuilder.Build(records).Splits;

        Assert.Equal([1, 2, 3], splits.Select(split => split.Number));
        Assert.Equal([1.0, 1.0, 0.5], splits.Select(split => split.DistanceKm));
        Assert.Equal([200, 200, 100], splits.Select(split => split.MovingSeconds));
        Assert.Equal([18.0, 18.0, 18.0], splits.Select(split => split.AverageSpeedKmh));
        Assert.Equal([10.0, 10.0, 5.0], splits.Select(split => split.ElevationGainMeters));
        Assert.All(splits, split => Assert.Equal((80.0, 200.0), (split.AverageCadence, split.AveragePowerWatts)));
    }

    [Fact]
    public void AShortLastStretchIsNotASplitOfItsOwn()
    {
        var records = Enumerable.Range(0, 22).Select(index => new TrackRecord(_start.AddSeconds(index * 10), DistanceMeters: Math.Min(index * 50, 1_060))).ToList();

        var splits = RideTrackBuilder.Build(records).Splits;

        Assert.Equal(1.0, Assert.Single(splits).DistanceKm);
    }

    [Fact]
    public void ARideWithoutDistanceHasNoChartsOrSplits()
    {
        TrackRecord[] records = [new(_start, 55.93, -3.12), new(_start.AddSeconds(5), 55.94, -3.13)];

        var track = RideTrackBuilder.Build(records);

        Assert.True(track.HasRoute);
        Assert.False(track.HasCharts);
        Assert.Empty(track.Splits);
    }
}
