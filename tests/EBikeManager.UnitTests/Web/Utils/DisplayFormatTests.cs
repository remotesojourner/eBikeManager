using System.Globalization;
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

            Assert.Equal("20.5 km", DisplayFormat.Distance(20_512));
            Assert.Equal("55 min", DisplayFormat.Duration(3_300));
            Assert.Equal("1 h 32 min", DisplayFormat.Duration(5_520));
            Assert.Equal("1 min", DisplayFormat.Duration(20));
            Assert.Equal("41 kcal", DisplayFormat.Calories(41.4));
            Assert.Equal("62%", DisplayFormat.Percent(62));
            Assert.Equal(DisplayFormat.Missing, DisplayFormat.Distance(null));
        }
        finally
        {
            CultureInfo.CurrentCulture = culture;
        }
    }
}
