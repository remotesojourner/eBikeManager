using EBikeManager.Application.Enums;
using EBikeManager.Application.Utils;

namespace EBikeManager.Web.Utils;

public sealed record ChartUnits(IReadOnlyDictionary<string, string> Labels, IReadOnlyDictionary<string, double> Factors)
{
    public static ChartUnits For(UnitSystem units) => new(
        new Dictionary<string, string>
        {
            ["distanceKm"] = UnitConversion.DistanceUnit(units),
            ["elevation"] = UnitConversion.ShortDistanceUnit(units),
            ["speed"] = UnitConversion.SpeedUnit(units),
            ["cadence"] = "rpm",
            ["power"] = "W",
            ["heartRate"] = "bpm"
        },
        new Dictionary<string, double>
        {
            ["distanceKm"] = UnitConversion.Distance(UnitConversion.MetresPerKilometre, units),
            ["elevation"] = UnitConversion.ShortDistance(1, units),
            ["speed"] = UnitConversion.Speed(1, units)
        });
}
