using System.Net;
using EBikeManager.Application.Configuration;
using EBikeManager.Web.Resources;

namespace EBikeManager.Web.Utils;

public static class MapSources
{
    public static MapSource For(MapSettings settings, bool dark) =>
        settings.StyleFor(dark) is { } style
            ? new MapSource(null, style, null, dark)
            : new MapSource(MapSettings.OpenStreetMapTiles, null, OpenStreetMapAttribution, dark);

    private static string OpenStreetMapAttribution =>
        $"<a href=\"{MapSettings.OpenStreetMapCopyright}\" target=\"_blank\" rel=\"noopener\">{WebUtility.HtmlEncode(WebStrings.MapAttributionOpenStreetMap)}</a>";
}
