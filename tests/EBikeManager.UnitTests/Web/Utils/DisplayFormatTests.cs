using System.Globalization;
using EBikeManager.Application.Enums;
using EBikeManager.Web.Utils;

namespace EBikeManager.UnitTests.Web.Utils;

public class DisplayFormatTests
{
    [Fact]
    public void RideFiguresAreShortAndReadable()
    {
        var culture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-GB");

            Assert.Equal("20.5 km", DisplayFormat.Distance(20_512, UnitSystem.Metric));
            Assert.Equal("55 min", DisplayFormat.Duration(3_300));
            Assert.Equal("1 h 32 min", DisplayFormat.Duration(5_520));
            Assert.Equal("1 min", DisplayFormat.Duration(20));
            Assert.Equal("41 kcal", DisplayFormat.Calories(41.4));
            Assert.Equal("62%", DisplayFormat.Percent(62));
            Assert.Equal(DisplayFormat.Missing, DisplayFormat.Distance(null, UnitSystem.Metric));
        }
        finally
        {
            CultureInfo.CurrentCulture = culture;
        }
    }

    [Fact]
    public void ImperialShowsMilesMphAndFeet()
    {
        var culture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-GB");

            Assert.Equal("12.7 mi", DisplayFormat.Distance(20_512, UnitSystem.Imperial));
            Assert.Equal("22,680 mi", DisplayFormat.WholeDistance(36_500_000, UnitSystem.Imperial));
            Assert.Equal("15.5 mph", DisplayFormat.Speed(25, UnitSystem.Imperial));
            Assert.Equal("394 ft", DisplayFormat.ShortDistance(120, UnitSystem.Imperial));
            Assert.Equal(DisplayFormat.Missing, DisplayFormat.Speed(null, UnitSystem.Imperial));
        }
        finally
        {
            CultureInfo.CurrentCulture = culture;
        }
    }

    [Fact]
    public void MetricShowsKilometresKmhAndMetres()
    {
        var culture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-GB");

            Assert.Equal("36,500 km", DisplayFormat.WholeDistance(36_500_000, UnitSystem.Metric));
            Assert.Equal("25 km/h", DisplayFormat.Speed(25, UnitSystem.Metric));
            Assert.Equal("27.4 km/h", DisplayFormat.Speed(27.4, UnitSystem.Metric));
            Assert.Equal("120 m", DisplayFormat.ShortDistance(120, UnitSystem.Metric));
            Assert.Equal("1.61", DisplayFormat.DistanceNumber(1_609.344, UnitSystem.Metric, "0.00"));
        }
        finally
        {
            CultureInfo.CurrentCulture = culture;
        }
    }
}
