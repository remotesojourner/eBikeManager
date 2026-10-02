using EBikeManager.Application.Enums;
using EBikeManager.Application.Utils;

namespace EBikeManager.UnitTests.Application.Utils;

public class UnitConversionTests
{
    [Fact]
    public void MetricKeepsKilometresKmhAndMetres()
    {
        Assert.Equal(20.5, UnitConversion.Distance(20_500, UnitSystem.Metric));
        Assert.Equal(25, UnitConversion.Speed(25, UnitSystem.Metric));
        Assert.Equal(120, UnitConversion.ShortDistance(120, UnitSystem.Metric));
        Assert.Equal(1_000, UnitConversion.SplitMetres(UnitSystem.Metric));
        Assert.Equal(("km", "km/h", "m"), (UnitConversion.DistanceUnit(UnitSystem.Metric), UnitConversion.SpeedUnit(UnitSystem.Metric), UnitConversion.ShortDistanceUnit(UnitSystem.Metric)));
    }

    [Fact]
    public void ImperialUsesMilesMphAndFeet()
    {
        Assert.Equal(1, UnitConversion.Distance(1_609.344, UnitSystem.Imperial), 9);
        Assert.Equal(20, UnitConversion.Speed(32.18688, UnitSystem.Imperial), 9);
        Assert.Equal(100, UnitConversion.ShortDistance(30.48, UnitSystem.Imperial), 9);
        Assert.Equal(1_609.344, UnitConversion.SplitMetres(UnitSystem.Imperial));
        Assert.Equal(("mi", "mph", "ft"), (UnitConversion.DistanceUnit(UnitSystem.Imperial), UnitConversion.SpeedUnit(UnitSystem.Imperial), UnitConversion.ShortDistanceUnit(UnitSystem.Imperial)));
    }
}
