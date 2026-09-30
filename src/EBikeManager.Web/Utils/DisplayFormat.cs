using System.Globalization;
using EBikeManager.Web.Resources;

namespace EBikeManager.Web.Utils;

public static class DisplayFormat
{
    public const string Missing = "–";

    public static string Distance(double? meters) =>
        meters is { } value ? WebStrings.Format(WebStrings.UnitKilometres, (value / 1000).ToString("0.0", CultureInfo.CurrentCulture)) : Missing;

    public static string Duration(double? seconds)
    {
        if (seconds is not { } value) return Missing;

        var total = (int)Math.Round(value);
        return total >= 3600
            ? WebStrings.Format(WebStrings.UnitHoursMinutes, total / 3600, total % 3600 / 60)
            : WebStrings.Format(WebStrings.UnitMinutes, Math.Max(1, total / 60));
    }

    public static string Kilometres(double? kilometres) =>
        kilometres is { } value ? WebStrings.Format(WebStrings.UnitKilometres, value.ToString("0", CultureInfo.CurrentCulture)) : Missing;

    public static string Speed(double? kilometresPerHour) =>
        kilometresPerHour is { } value ? WebStrings.Format(WebStrings.UnitKilometresPerHour, value.ToString("0.#", CultureInfo.CurrentCulture)) : Missing;

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
}
