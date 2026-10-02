using EBikeManager.Application.Models.Dtos;
using EBikeManager.Web.Utils;

namespace EBikeManager.UnitTests.Web.Utils;

public class RouteColoursTests
{
    private static readonly RideSeriesDto _withoutHeartRate = new([0, 1], [10, 12], [18, 20], [70, 80], [100, 150], [null, null], [51.5, 51.6], [-0.12, -0.13]);

    [Fact]
    public void OnlyValuesTheRideHasCanColourItsRoute()
    {
        Assert.Equal([RouteColours.Plain, RouteColours.Speed, RouteColours.Power, RouteColours.Cadence, RouteColours.Elevation], RouteColours.For(_withoutHeartRate));
        Assert.Equal([RouteColours.Plain], RouteColours.For(RideSeriesDto.Empty));
    }

    [Fact]
    public void TheLastChoiceIsKeptWhenTheRideHasItAndSpeedIsTheDefault()
    {
        var options = RouteColours.For(_withoutHeartRate);

        Assert.Equal(RouteColours.Power, RouteColours.Choose(RouteColours.Power, options));
        Assert.Equal(RouteColours.Plain, RouteColours.Choose(RouteColours.Plain, options));
        Assert.Equal(RouteColours.Speed, RouteColours.Choose(RouteColours.HeartRate, options));
        Assert.Equal(RouteColours.Speed, RouteColours.Choose(null, options));
        Assert.Equal(RouteColours.Plain, RouteColours.Choose(null, RouteColours.For(RideSeriesDto.Empty)));
        Assert.Null(RouteColours.ForMap(RouteColours.Plain));
    }
}
