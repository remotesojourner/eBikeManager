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
            new(_start, Latitude: 51.5, Longitude: -0.12),
            new(_start, AltitudeMeters: 50, SpeedMetresPerSecond: 5, Cadence: 60, PowerWatts: 100, DistanceMeters: 0),
            new(_start.AddSeconds(1), DistanceMeters: 5),
            new(_start.AddSeconds(2), Latitude: 51.5001, Longitude: -0.1201),
            new(_start.AddSeconds(2), SpeedMetresPerSecond: 6, DistanceMeters: 10)
        ];

        var track = RideTrackBuilder.Build(records);

        Assert.Equal([[-0.12, 51.5], [-0.1201, 51.5001]], track.Route);
        Assert.Equal([0, 0.005, 0.01], track.Series.DistanceKm);
        Assert.Equal([18, 18, 21.6], track.Series.Speed);
        Assert.Equal([50, 50, 50], track.Series.Elevation);
        Assert.Equal([100, 100, 100], track.Series.Power);
        Assert.Equal([51.5, 51.5, 51.5001], track.Series.Latitude);
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
    public void MileSplitsEndAtEachMileAndKeepALastPartMile()
    {
        var records = Enumerable.Range(0, 81).Select(index => new TrackRecord(_start.AddSeconds(index * 10), DistanceMeters: index * 50)).ToList();

        var splits = RideTrackBuilder.Build(records, UnitConversion.MetresPerMile).Splits;

        Assert.Equal([1.65, 1.6, 0.75], splits.Select(split => split.DistanceKm));
        Assert.Equal(4.0, splits.Sum(split => split.DistanceKm), 2);
    }

    [Fact]
    public void ALastStretchUnderATenthOfAMileIsNotASplitOfItsOwn()
    {
        var records = Enumerable.Range(0, 36).Select(index => new TrackRecord(_start.AddSeconds(index * 10), DistanceMeters: Math.Min(index * 50, 1_750))).ToList();

        var splits = RideTrackBuilder.Build(records, UnitConversion.MetresPerMile).Splits;

        Assert.Equal(1.65, Assert.Single(splits).DistanceKm);
    }

    [Fact]
    public void EveryRoutePointCarriesItsValuesWithEffortSmoothedOverAFewSeconds()
    {
        var records = Enumerable.Range(0, 21).Select(second => new TrackRecord(
            _start.AddSeconds(second), Latitude: 51.5 + second * 0.0001, Longitude: -0.12, AltitudeMeters: 10 + second,
            SpeedMetresPerSecond: 5, Cadence: 80, PowerWatts: second % 2 == 0 ? 200 : 0, DistanceMeters: second * 5)).ToList();

        var track = RideTrackBuilder.Build(records);

        var values = track.RouteValues;
        Assert.Equal(21, track.Route.Count);
        Assert.All(values.Speed, speed => Assert.Equal(18, speed));
        Assert.All(values.Cadence, cadence => Assert.Equal(80, cadence));
        Assert.All(values.Power, power => Assert.InRange(power!.Value, 80, 120));
        Assert.All(values.Gradient, gradient => Assert.Equal(20, gradient));
        Assert.All(values.HeartRate, Assert.Null);
    }

    [Fact]
    public void ThinningTheRouteKeepsEachPointsValuesWithIt()
    {
        var records = Enumerable.Range(0, 10_000).Select(second => new TrackRecord(
            _start.AddSeconds(second), Latitude: 51 + second * 0.00001, Longitude: -0.12, SpeedMetresPerSecond: second / 1000.0, DistanceMeters: second)).ToList();

        var track = RideTrackBuilder.Build(records);

        Assert.Equal(RideTrackBuilder.MaxRoutePoints, track.Route.Count);
        Assert.Equal(RideTrackBuilder.MaxRoutePoints, track.RouteValues.Speed.Count);
        var seconds = track.Route.Select(position => (int)Math.Round((position[1] - 51) / 0.00001)).ToList();
        Assert.All(seconds.Zip(track.RouteValues.Speed).Where(pair => pair.First is > 2 and < 9_997),
            pair => Assert.Equal(pair.First / 1000.0 * 3.6, pair.Second!.Value, 0.051));
    }

    [Fact]
    public void TheGradientIsTheClimbOverTheSurroundingHundredMetres()
    {
        var records = Enumerable.Range(0, 81).Select(index => new TrackRecord(
            _start.AddSeconds(index), Latitude: 51.5 + index * 0.0001, Longitude: -0.12,
            AltitudeMeters: index <= 40 ? 10 + index * 0.5 : 30 - (index - 40) * 0.25, DistanceMeters: index * 5)).ToList();

        var gradient = RideTrackBuilder.Build(records).RouteValues.Gradient;

        Assert.Equal(10, gradient[15]);
        Assert.Equal(-5, gradient[65]);
        Assert.InRange(gradient[40]!.Value, -5, 10);
    }

    [Fact]
    public void ARideWithoutDistanceHasNoChartsOrSplits()
    {
        TrackRecord[] records = [new(_start, 51.5, -0.12), new(_start.AddSeconds(5), 51.51, -0.13)];

        var track = RideTrackBuilder.Build(records);

        Assert.True(track.HasRoute);
        Assert.False(track.HasCharts);
        Assert.Empty(track.Splits);
    }
}
