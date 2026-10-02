using System.Globalization;
using EBikeManager.Application.Enums;
using EBikeManager.Application.Utils;
using EBikeManager.Web.Resources;

namespace EBikeManager.Web.Utils;

public static class DisplayFormat
{
    public const string Missing = "–";

    public const string UnitsCascade = "Units";

    public static string Distance(double? meters, UnitSystem units) =>
        WithUnit(DistanceNumber(meters, units, "0.0"), UnitConversion.DistanceUnit(units));

    public static string WholeDistance(double? meters, UnitSystem units) =>
        WithUnit(DistanceNumber(meters, units, "#,0"), UnitConversion.DistanceUnit(units));

    public static string DistanceNumber(double? meters, UnitSystem units, string format) =>
        meters is { } value ? Number(UnitConversion.Distance(value, units), format) : Missing;

    public static string Speed(double? kilometresPerHour, UnitSystem units) =>
        WithUnit(SpeedNumber(kilometresPerHour, units, "0.#"), UnitConversion.SpeedUnit(units));

    public static string SpeedNumber(double? kilometresPerHour, UnitSystem units, string format) =>
        kilometresPerHour is { } value ? Number(UnitConversion.Speed(value, units), format) : Missing;

    public static string ShortDistance(double? meters, UnitSystem units) =>
        WithUnit(meters is { } value ? Number(UnitConversion.ShortDistance(value, units), "#,0") : Missing, UnitConversion.ShortDistanceUnit(units));

    public static string Duration(double? seconds)
    {
        if (seconds is not { } value) return Missing;

        var total = (int)Math.Round(value);
        return total >= 3600
            ? WebStrings.Format(WebStrings.UnitHoursMinutes, total / 3600, total % 3600 / 60)
            : WebStrings.Format(WebStrings.UnitMinutes, Math.Max(1, total / 60));
    }

    public static string WattHours(double? wattHours) =>
        wattHours is { } value ? WebStrings.Format(WebStrings.UnitWattHours, value.ToString("#,0", CultureInfo.CurrentCulture)) : Missing;

    public static string KilowattHours(double? wattHours) =>
        wattHours is { } value ? WebStrings.Format(WebStrings.UnitKilowattHours, (value / 1000).ToString("#,0.#", CultureInfo.CurrentCulture)) : Missing;

    public static string Calories(double? kcal) =>
        kcal is { } value ? WebStrings.Format(WebStrings.UnitKilocalories, value.ToString("0", CultureInfo.CurrentCulture)) : Missing;

    public static string Number(double? value, string format = "0") =>
        value is { } number ? number.ToString(format, CultureInfo.CurrentCulture) : Missing;

    public static string Percent(double? value) =>
        value is { } number ? WebStrings.Format(WebStrings.UnitPercent, number.ToString("0", CultureInfo.CurrentCulture)) : Missing;

    public static string DateTime(DateTime value) => value.ToString("d MMM yyyy, HH:mm", CultureInfo.CurrentCulture);

    public static string Date(DateTime value) => value.ToString("d MMM yyyy", CultureInfo.CurrentCulture);

    private static string WithUnit(string number, string unit) => number == Missing ? Missing : WebStrings.Format(WebStrings.UnitValue, number, unit);
}
