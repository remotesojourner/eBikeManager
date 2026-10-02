using EBikeManager.Application.Enums;
using EBikeManager.Application.Models.Dtos;
using EBikeManager.Web.Resources;

namespace EBikeManager.Web.Utils;

public static class RouteColours
{
    public const string Plain = "plain";
    public const string Speed = "speed";
    public const string Power = "power";
    public const string Cadence = "cadence";
    public const string Gradient = "gradient";
    public const string HeartRate = "heartRate";
    public const string StorageKey = "routeColour";

    public static IReadOnlyList<string> For(RideSeriesDto series)
    {
        var options = new List<string> { Plain };
        if (series.HasSpeed) options.Add(Speed);
        if (series.HasPower) options.Add(Power);
        if (series.HasCadence) options.Add(Cadence);
        if (series.HasElevation) options.Add(Gradient);
        if (series.HasHeartRate) options.Add(HeartRate);
        return options;
    }

    public static string Choose(string? stored, IReadOnlyList<string> options) =>
        stored != null && options.Contains(stored) ? stored : options.Contains(Speed) ? Speed : Plain;

    public static string? ForMap(string colour) => colour == Plain ? null : colour;

    public static IReadOnlyDictionary<string, RouteScale> Scales(UnitSystem units) => new Dictionary<string, RouteScale>
    {
        [Speed] = units == UnitSystem.Imperial ? new RouteScale(0, 22) : new RouteScale(0, 35),
        [Power] = new RouteScale(0, 300),
        [Cadence] = new RouteScale(0, 100),
        [Gradient] = new RouteScale(-10, 10),
        [HeartRate] = new RouteScale(60, 180)
    };

    public static string Label(string colour) => colour switch
    {
        Speed => WebStrings.RideSpeed,
        Power => WebStrings.RidePower,
        Cadence => WebStrings.RideCadence,
        Gradient => WebStrings.RideGradient,
        HeartRate => WebStrings.RideHeartRate,
        _ => WebStrings.RideRouteColourPlain
    };
}
