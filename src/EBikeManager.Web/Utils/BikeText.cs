using System.Globalization;
using EBikeManager.Application.Enums;
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

    public static string MapUrl(double latitude, double longitude) =>
        string.Create(CultureInfo.InvariantCulture, $"https://www.openstreetmap.org/?mlat={latitude:0.######}&mlon={longitude:0.######}#map=17/{latitude:0.######}/{longitude:0.######}");

    public static string Coordinates(double latitude, double longitude) =>
        string.Create(CultureInfo.InvariantCulture, $"{latitude:0.#####}, {longitude:0.#####}");
}
