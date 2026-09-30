using EBikeManager.Application.Enums;
using EBikeManager.Application.Models.Entities;
using EBikeManager.Application.Utils;
using EBikeManager.TestSupport;

namespace EBikeManager.UnitTests.Application.Utils;

public class BikeProfileParserTests
{
    private static readonly DateTime _updated = new(2026, 9, 30, 13, 17, 0, DateTimeKind.Utc);

    [Fact]
    public void ReadsTheBikeProfileLikeTheFlowAppShowsIt()
    {
        var bike = NewBike(BoschSamples.Profile("bike-1"));
        var names = BoschJson.AssistModeNames([BoschSamples.RideSummary("Ride")]);

        var details = BikeProfileParser.Parse(bike, names, _updated);

        Assert.True(details.HasDetails);
        Assert.Equal("TENWAYS", details.Brand);
        Assert.Equal(_updated, details.PictureSavedAt);
        Assert.Equal("WTEN123456789", details.FrameNumber);
        Assert.Equal(36445, details.OdometerMeters);
        Assert.Equal((9, 8), (details.MotorHours, details.MotorHoursAssisted));
        Assert.Equal(27.4, details.MaxAssistSpeedKmh);
        Assert.True(details.LockEnabled);
        Assert.Null(details.AlarmEnabled);
        Assert.Null(details.ServiceDue);
        Assert.False(details.HasFlowPlus);

        var battery = Assert.Single(details.Batteries);
        Assert.Equal(("PowerTube 540", 535.6, 0.9, 305.0), (battery.ProductName, battery.CapacityWh, battery.ChargeCycles, battery.LifetimeDeliveredWh));
        Assert.Null(battery.LevelPercent);

        Assert.Equal(
            [BikeComponentKind.DriveUnit, BikeComponentKind.HeadUnit, BikeComponentKind.RemoteControl],
            details.Components.Select(component => component.Kind));
        Assert.Equal("Kiox 300", details.Components[1].ProductName);
    }

    [Fact]
    public void AssistModesAreNamedFromRideSummariesAndOffIsLeftOut()
    {
        var details = BikeProfileParser.Parse(NewBike(BoschSamples.Profile("bike-1")), BoschJson.AssistModeNames([BoschSamples.RideSummary("Ride")]), null);

        Assert.Equal(["ECO", "AUTO", "SPORT", "TURBO"], details.AssistModes.Select(mode => mode.Name));
        Assert.Equal([33.0, 25.0, 16.0, 12.0], details.AssistModes.Select(mode => mode.ReachableRangeKm ?? 0));
        Assert.Equal("#78BE20", details.AssistModes[0].Color);
    }

    [Fact]
    public void WithoutRideSummariesModesKeepTheirBoschIds()
    {
        var details = BikeProfileParser.Parse(NewBike(BoschSamples.Profile("bike-1")));

        Assert.Equal(["A100M40040", "A100E3AUTO", "A100M40020", "A100M40010"], details.AssistModes.Select(mode => mode.Name));
    }

    [Fact]
    public void ConnectModuleDataAddsTheLiveBatteryAndLocation()
    {
        var bike = NewBike(BoschSamples.Profile("bike-1"));
        bike.StateOfChargeJson = BoschSamples.StateOfCharge;
        bike.LocationJson = BoschSamples.Location;

        var details = BikeProfileParser.Parse(bike);

        Assert.Equal(36500, details.OdometerMeters);
        Assert.Equal(76, details.LiveState!.ChargePercent);
        Assert.Equal((22, 61), (details.LiveState.MinRangeKm, details.LiveState.MaxRangeKm));
        Assert.Equal(402, details.LiveState.RemainingWh);
        Assert.Equal(55.953251, details.LastLocation!.Latitude);
        Assert.Equal(new DateTime(2026, 9, 29, 18, 21, 0, DateTimeKind.Utc), details.LastLocation.DetectedAt);
    }

    [Fact]
    public void ABikeThatWasNeverRefreshedHasNoDetails()
    {
        var details = BikeProfileParser.Parse(new Bike { Id = "bike-1", Name = "TENWAYS (Performance Line)" });

        Assert.False(details.HasDetails);
        Assert.Empty(details.Components);
        Assert.Null(details.LiveState);
    }

    private static Bike NewBike(string profile) => new()
    {
        Id = "bike-1",
        Name = "TENWAYS (Performance Line)",
        ProfileJson = profile,
        PassJson = BoschSamples.BikePass("bike-1"),
        HasFlowPlus = false,
        DetailsUpdatedAt = _updated
    };
}
