using System.Globalization;
using EBikeManager.Application.Enums;
using EBikeManager.Application.Models.Dtos;
using EBikeManager.Web.Resources;

namespace EBikeManager.Web.Utils;

public static class BikeText
{
    public static string ComponentName(BikeComponentKind kind) => kind switch
    {
        BikeComponentKind.DriveUnit => WebStrings.BikeComponentDriveUnit,
        BikeComponentKind.HeadUnit => WebStrings.BikeComponentHeadUnit,
        BikeComponentKind.RemoteControl => WebStrings.BikeComponentRemoteControl,
        BikeComponentKind.ConnectModule => WebStrings.BikeComponentConnectModule,
        BikeComponentKind.AntiLockBrakeSystem => WebStrings.BikeComponentAbs,
        _ => kind.ToString()
    };

    public static string DocumentName(BikeDocumentKind kind) => kind switch
    {
        BikeDocumentKind.BikePhoto => WebStrings.BikeDocumentBikePhoto,
        BikeDocumentKind.BikeInvoice => WebStrings.BikeDocumentBikeInvoice,
        BikeDocumentKind.LockInvoice => WebStrings.BikeDocumentLockInvoice,
        _ => WebStrings.BikeDocumentOther
    };

    public static string YesNo(bool? value) => value switch
    {
        true => WebStrings.Yes,
        false => WebStrings.No,
        _ => DisplayFormat.Missing
    };

    public static string OnOff(bool? value) => value switch
    {
        true => WebStrings.On,
        false => WebStrings.Off,
        _ => DisplayFormat.Missing
    };

    public static string ModeBackground(string? colour, bool isOff = false) =>
        isOff ? "background-color:var(--mud-palette-text-disabled)"
        : colour != null ? $"background-color:{colour}"
        : "background-color:var(--mud-palette-primary)";

    public static string MapUrl(double latitude, double longitude) =>
        string.Create(CultureInfo.InvariantCulture, $"https://www.openstreetmap.org/?mlat={latitude:0.######}&mlon={longitude:0.######}#map=17/{latitude:0.######}/{longitude:0.######}");

    public static string Coordinates(double latitude, double longitude) =>
        string.Create(CultureInfo.InvariantCulture, $"{latitude:0.#####}, {longitude:0.#####}");

    public static string WithModel(string name, string? model) =>
        string.IsNullOrWhiteSpace(model) || string.Equals(name, model, StringComparison.OrdinalIgnoreCase)
            ? name
            : WebStrings.Format(WebStrings.BikeNameWithModel, name, model);

    public static IReadOnlyDictionary<string, string> Labels(IEnumerable<(string Id, string Name, string? Model)> bikes)
    {
        var distinct = bikes.DistinctBy(bike => bike.Id).ToList();
        return distinct.ToDictionary(
            bike => bike.Id,
            bike => distinct.Count(other => string.Equals(other.Name, bike.Name, StringComparison.OrdinalIgnoreCase)) > 1
                ? WithModel(bike.Name, bike.Model)
                : bike.Name);
    }

    public static double? ChargeLevel(BikeDetailsDto bike, BikeBatteryDto battery, BridgeReadingsDto? live) =>
        bike.LiveState?.ChargePercent ?? live?.BatteryPercent ?? bike.BridgeBatteryPercent ?? battery.LevelPercent;

    public static bool ChargeLevelIsStored(BikeDetailsDto bike, BridgeReadingsDto? live) =>
        bike.LiveState?.ChargePercent == null && live?.BatteryPercent == null && bike.BridgeBatteryPercent != null;

    public static double? OdometerMeters(BikeDetailsDto bike, BridgeReadingsDto? live) =>
        live?.OdometerKm * 1000 is { } liveMeters && !(bike.OdometerMeters >= liveMeters) ? liveMeters : bike.OdometerMeters;
}
