using System.Globalization;

namespace EBikeManager.Web.Utils;

public static class BikePictureEndpoint
{
    public const string CacheControl = "private, max-age=31536000, immutable";

    public static string PathFor(string bikeId, DateTime savedAt) =>
        string.Create(CultureInfo.InvariantCulture, $"bikes/{Uri.EscapeDataString(bikeId)}/picture?v={savedAt.Ticks}");
}
