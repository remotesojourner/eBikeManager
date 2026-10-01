using EBikeManager.Application.Models.Entities;
using EBikeManager.Application.Utils;
using EBikeManager.TestSupport;

namespace EBikeManager.UnitTests.Application.Utils;

public class RideDetailParserTests
{
    private static readonly DateTime _start = new(2026, 9, 6, 16, 12, 0, DateTimeKind.Utc);

    [Fact]
    public void ReadsTheRideLikeTheFlowAppShowsIt()
    {
        var ride = new Ride
        {
            Id = "ride-1",
            Title = "London roundtrip",
            BikeName = "Commuter",
            BikeModel = "TENWAYS (Performance Line)",
            StartTime = _start,
            EndTime = _start.AddMinutes(57),
            TimeZone = "Europe/London",
            DistanceMeters = 3_064,
            MovingSeconds = 666,
            CaloriesKcal = 43,
            FitPath = "2026/09/ride-1.fit",
            SummaryJson = """
                {
                  "maximumSpeed": 32.9, "averageCadence": 76.0, "maximumCadence": 106.0,
                  "averageRiderPower": 189.0, "maximumRiderPower": 412.0, "averageHeartRate": null,
                  "elevationGain": 12, "elevationLoss": 13, "riderEnergyShare": 35,
                  "co2EmissionsGrams": 9.2, "co2EmissionsCarEquivalentGrams": 509.0
                }
                """
        };

        var detail = RideDetailParser.Parse(ride, null, "No track");

        Assert.Equal(("Commuter", "TENWAYS (Performance Line)"), (detail.BikeName, detail.BikeModel));
        Assert.Equal(new DateTime(2026, 9, 6, 17, 12, 0, DateTimeKind.Unspecified), detail.LocalStartTime);
        Assert.Equal((3_064.0, 666.0, 3_420.0), (detail.DistanceMeters, detail.MovingSeconds, detail.ElapsedSeconds));
        Assert.Equal((32.9, 76.0, 106.0), (detail.MaximumSpeedKmh, detail.AverageCadence, detail.MaximumCadence));
        Assert.Equal((189.0, 412.0), (detail.AverageRiderPowerWatts, detail.MaximumRiderPowerWatts));
        Assert.Null(detail.AverageHeartRate);
        Assert.Equal((12.0, 13.0, 35.0), (detail.ElevationGainMeters, detail.ElevationLossMeters, detail.RiderEnergySharePercent));
        Assert.Equal((9.2, 509.0), (detail.Co2Grams, detail.Co2CarGrams));
        Assert.Equal(43, detail.CaloriesKcal);
        Assert.True(detail.HasFit);
        Assert.Equal("No track", detail.TrackProblem);
    }

    [Fact]
    public void AssistModesShowTheShareOfTheDistanceWithBoschsColours()
    {
        var ride = new Ride { Id = "ride-1", StartTime = _start, SummaryJson = BoschSamples.RideSummary("Ride") };

        var modes = RideDetailParser.Parse(ride, null, null).AssistModes;

        Assert.Equal(["TURBO", "ECO"], modes.Select(mode => mode.Name));
        Assert.Equal(("#E20015", 1_656.0, 93.2), (modes[0].Color, modes[0].Meters, modes[0].Percent));
        Assert.Equal(("#78BE20", 6.8), (modes[1].Color, modes[1].Percent));
    }

    [Fact]
    public void ASummaryThatIsNotJsonStillGivesTheStoredFigures()
    {
        var ride = new Ride { Id = "ride-1", StartTime = _start, DistanceMeters = 1_000, SummaryJson = "not json" };

        var detail = RideDetailParser.Parse(ride, null, null);

        Assert.Equal(1_000, detail.DistanceMeters);
        Assert.Null(detail.MaximumSpeedKmh);
        Assert.Empty(detail.AssistModes);
        Assert.False(detail.HasFit);
    }
}
