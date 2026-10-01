using EBikeManager.Web.Utils;

namespace EBikeManager.UnitTests.Web.Utils;

public class SampleRouteTests
{
    private const double EarthRadiusMetres = 6_371_000;

    [Fact]
    public void TheSampleRideIsAboutTwoKilometresInCentralLondon()
    {
        var positions = SampleRoute.Positions;

        Assert.All(positions, position =>
        {
            Assert.InRange(position[0], -0.13, -0.10);
            Assert.InRange(position[1], 51.50, 51.52);
        });
        Assert.InRange(positions.Zip(positions.Skip(1), DistanceMetres).Sum(), 1_900, 2_200);
    }

    private static double DistanceMetres(double[] from, double[] to)
    {
        var latitudeFrom = double.DegreesToRadians(from[1]);
        var latitudeTo = double.DegreesToRadians(to[1]);
        var latitudeDelta = latitudeTo - latitudeFrom;
        var longitudeDelta = double.DegreesToRadians(to[0] - from[0]);
        var haversine = Math.Pow(Math.Sin(latitudeDelta / 2), 2) + Math.Cos(latitudeFrom) * Math.Cos(latitudeTo) * Math.Pow(Math.Sin(longitudeDelta / 2), 2);
        return 2 * EarthRadiusMetres * Math.Asin(Math.Sqrt(haversine));
    }
}
