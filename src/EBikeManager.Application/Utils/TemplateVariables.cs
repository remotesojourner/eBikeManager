using System.Globalization;
using EBikeManager.Application.Enums;
using EBikeManager.Application.Models.Dtos;
using EBikeManager.Application.Models.Events;

namespace EBikeManager.Application.Utils;

internal static class TemplateVariables
{
    private const string Missing = "-";
    private const string UntitledRide = "Untitled ride";

    public static Dictionary<string, string> For(NotificationEvent notificationEvent, UnitSystem units) => notificationEvent switch
    {
        RideSynced synced => From(synced.Ride, units),
        UploadFailed failed => new(From(failed.Ride, units), StringComparer.OrdinalIgnoreCase) { ["service"] = failed.Service, ["error"] = failed.Error },
        SyncFailed failed => new(StringComparer.OrdinalIgnoreCase) { ["error"] = failed.Error },
        SignInRequired required => new(StringComparer.OrdinalIgnoreCase) { ["service"] = required.Service },
        _ => new(StringComparer.OrdinalIgnoreCase)
    };

    private static Dictionary<string, string> From(RideDto ride, UnitSystem units) => new(StringComparer.OrdinalIgnoreCase)
    {
        ["title"] = string.IsNullOrWhiteSpace(ride.Title) ? UntitledRide : ride.Title,
        ["bike"] = ride.BikeName ?? ride.BikeModel ?? Missing,
        ["date"] = ride.LocalStartTime.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture),
        ["distance"] = ride.DistanceMeters is { } metres ? WithUnit(UnitConversion.Distance(metres, units), "0.0", UnitConversion.DistanceUnit(units)) : Missing,
        ["moving_time"] = ride.MovingSeconds is { } seconds ? Duration(seconds) : Missing,
        ["average_speed"] = ride.AverageSpeedKmh is { } speed ? WithUnit(UnitConversion.Speed(speed, units), "0.0", UnitConversion.SpeedUnit(units)) : Missing,
        ["elevation"] = ride.ElevationGainMeters is { } climb ? WithUnit(UnitConversion.ShortDistance(climb, units), "0", UnitConversion.ShortDistanceUnit(units)) : Missing,
        ["calories"] = ride.CaloriesKcal is { } kcal ? WithUnit(kcal, "0", "kcal") : Missing,
        ["rider_share"] = ride.RiderEnergySharePercent is { } share ? share.ToString(CultureInfo.InvariantCulture) + "%" : Missing
    };

    private static string WithUnit(double value, string format, string unit) => $"{value.ToString(format, CultureInfo.InvariantCulture)} {unit}";

    private static string Duration(int seconds)
    {
        var minutes = (int)Math.Round(seconds / 60.0);
        return minutes >= 60
            ? string.Create(CultureInfo.InvariantCulture, $"{minutes / 60} h {minutes % 60} min")
            : string.Create(CultureInfo.InvariantCulture, $"{Math.Max(1, minutes)} min");
    }
}
