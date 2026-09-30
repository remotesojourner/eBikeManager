using EBikeManager.Application.Utils;

namespace EBikeManager.UnitTests.Application.Utils;

public class TimeZonesTests
{
    [Fact]
    public void RidesAreShownInTheTimeZoneTheyHappenedIn()
    {
        var utc = new DateTime(2026, 9, 30, 8, 43, 0, DateTimeKind.Utc);

        Assert.Equal(new DateTime(2026, 9, 30, 9, 43, 0), TimeZones.ToRideLocal(utc, "Europe/London"));
        Assert.Equal(new DateTime(2026, 9, 30, 8, 43, 0), TimeZones.ToRideLocal(utc, "Not/AZone"));
        Assert.Equal(new DateTime(2026, 9, 30, 8, 43, 0), TimeZones.ToRideLocal(utc, null));
    }
}
