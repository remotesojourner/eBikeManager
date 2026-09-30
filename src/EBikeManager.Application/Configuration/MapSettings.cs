using EBikeManager.Application.Enums;

namespace EBikeManager.Application.Configuration;

public sealed record MapSettings(MapProvider Provider, string? StyleUrl, string? DarkStyleUrl)
{
    public const string OpenStreetMapKey = "openStreetMap";
    public const string OpenFreeMapKey = "openFreeMap";
    public const string CustomKey = "custom";

    public const string OpenStreetMapTiles = "https://tile.openstreetmap.org/{z}/{x}/{y}.png";
    public const string OpenStreetMapCopyright = "https://www.openstreetmap.org/copyright";
    public const string OpenFreeMapStyle = "https://tiles.openfreemap.org/styles/liberty";
    public const string OpenFreeMapDarkStyle = "https://tiles.openfreemap.org/styles/dark";

    public static string KeyFor(MapProvider provider) => provider switch
    {
        MapProvider.OpenFreeMap => OpenFreeMapKey,
        MapProvider.Custom => CustomKey,
        _ => OpenStreetMapKey
    };

    public static MapProvider? ProviderFor(string? key) => key switch
    {
        OpenStreetMapKey => MapProvider.OpenStreetMap,
        OpenFreeMapKey => MapProvider.OpenFreeMap,
        CustomKey => MapProvider.Custom,
        _ => null
    };

    public static bool IsValidStyleUrl(string? url) =>
        Uri.TryCreate(url?.Trim(), UriKind.Absolute, out var address) && (address.Scheme == Uri.UriSchemeHttps || address.Scheme == Uri.UriSchemeHttp);

    public static MapSettings From(string? provider, string? styleUrl, string? darkStyleUrl)
    {
        var style = IsValidStyleUrl(styleUrl) ? styleUrl!.Trim() : null;
        var darkStyle = IsValidStyleUrl(darkStyleUrl) ? darkStyleUrl!.Trim() : null;
        var chosen = ProviderFor(provider) ?? MapProvider.OpenStreetMap;
        return new MapSettings(chosen == MapProvider.Custom && style == null ? MapProvider.OpenStreetMap : chosen, style, darkStyle);
    }

    public string? StyleFor(bool dark) => Provider switch
    {
        MapProvider.OpenFreeMap => dark ? OpenFreeMapDarkStyle : OpenFreeMapStyle,
        MapProvider.Custom => dark ? DarkStyleUrl ?? StyleUrl : StyleUrl,
        _ => null
    };
}
