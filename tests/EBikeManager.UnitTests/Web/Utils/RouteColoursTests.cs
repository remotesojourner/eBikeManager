using EBikeManager.Application.Enums;
using EBikeManager.Application.Models.Dtos;
using EBikeManager.Web.Utils;

namespace EBikeManager.UnitTests.Web.Utils;

public class RouteColoursTests
{
    private static readonly RideSeriesDto _withoutHeartRate = new([0, 1], [10, 12], [18, 20], [70, 80], [100, 150], [null, null], [51.5, 51.6], [-0.12, -0.13]);

    [Fact]
    public void OnlyValuesTheRideHasCanColourItsRoute()
    {
        Assert.Equal([RouteColours.Plain, RouteColours.Speed, RouteColours.Power, RouteColours.Cadence, RouteColours.Gradient], RouteColours.For(_withoutHeartRate));
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

    [Fact]
    public void EveryRideUsesTheSameScaleSoAColourAlwaysMeansTheSame()
    {
        var metric = RouteColours.Scales(UnitSystem.Metric);
        var imperial = RouteColours.Scales(UnitSystem.Imperial);

        Assert.Equal(new RouteScale(0, 35), metric[RouteColours.Speed]);
        Assert.Equal(new RouteScale(0, 22), imperial[RouteColours.Speed]);
        Assert.Equal(new RouteScale(0, 300), metric[RouteColours.Power]);
        Assert.Equal(new RouteScale(-10, 10), imperial[RouteColours.Gradient]);
        Assert.All(RouteColours.For(new RideSeriesDto([0, 1], [10, 12], [18, 20], [70, 80], [100, 150], [120, 130], [51.5, 51.6], [-0.12, -0.13])).Skip(1),
            colour => Assert.True(metric.ContainsKey(colour), colour));
    }
}
