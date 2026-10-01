using EBikeManager.Web.Resources;

namespace EBikeManager.Web.Utils;

public static class RideText
{
    public static string StartAndBike(DateTime localStartTime, string? bikeName) =>
        bikeName is { } bike
            ? WebStrings.Format(WebStrings.RideSubtitle, DisplayFormat.DateTime(localStartTime), bike)
            : DisplayFormat.DateTime(localStartTime);
}
