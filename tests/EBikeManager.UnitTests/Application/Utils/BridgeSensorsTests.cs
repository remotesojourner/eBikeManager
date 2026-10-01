using EBikeManager.Application.Enums;
using EBikeManager.Application.Models;
using EBikeManager.Application.Models.Dtos;
using EBikeManager.Application.Utils;

namespace EBikeManager.UnitTests.Application.Utils;

public class BridgeSensorsTests
{
    [Theory]
    [InlineData("eBike Battery SoC (Live)", 1, BridgeReading.BatteryPercent)]
    [InlineData("eBike Odometer (Live)", 1, BridgeReading.Odometer)]
    [InlineData("eBike Charge Time to 80% (Estimate)", 1, BridgeReading.ChargeTimeTo80)]
    [InlineData("eBike 1 Speed", 1, BridgeReading.Speed)]
    [InlineData("eBike 2 System Locked", 2, BridgeReading.Locked)]
    [InlineData("eBike 2 In Motion", 2, BridgeReading.InMotion)]
    public void TheBridgesSensorsAreRecognisedForEachBike(string name, int slot, BridgeReading reading)
    {
        Assert.Equal(new BridgeSensor(slot, reading), BridgeSensors.Find(name));
    }

    [Theory]
    [InlineData("WiFi Signal")]
    [InlineData("eBike Advertising")]
    [InlineData("eBike 3 Speed")]
    public void OtherEntitiesAreIgnored(string name)
    {
        Assert.Null(BridgeSensors.Find(name));
    }

    [Fact]
    public void TheLockSensorIsInvertedBecauseTheBridgeReportsUnlockedAsOn()
    {
        var readings = BridgeReadingsDto.Empty(1, "bike-1");

        Assert.True(BridgeSensors.Apply(readings, BridgeReading.Locked, null, false).Locked);
        Assert.False(BridgeSensors.Apply(readings, BridgeReading.Locked, null, true).Locked);
        Assert.Null(BridgeSensors.Apply(readings, BridgeReading.Locked, null, null).Locked);
        Assert.Equal(50, BridgeSensors.Apply(readings, BridgeReading.BatteryPercent, 50f, null).BatteryPercent);
        Assert.True(BridgeSensors.Apply(readings, BridgeReading.Connected, null, true).Connected);
    }
}
