using EBikeManager.Web.Resources;

namespace EBikeManager.Web.Utils;

public sealed record MapTexts(string Hint, string MapFailed, string MapUnavailable, IReadOnlyDictionary<string, string> Labels)
{
    public static MapTexts Current => new(
        WebStrings.RideChartHint,
        WebStrings.MapFailed,
        WebStrings.MapUnavailable,
        new Dictionary<string, string>
        {
            ["distanceKm"] = WebStrings.Distance,
            ["elevation"] = WebStrings.RideElevation,
            ["speed"] = WebStrings.RideSpeed,
            ["cadence"] = WebStrings.RideCadence,
            ["power"] = WebStrings.RidePower,
            ["heartRate"] = WebStrings.RideHeartRate,
            ["gradient"] = WebStrings.RideGradient
        });
}
