using System.Globalization;
using EBikeManager.Application.Models.Dtos;

namespace EBikeManager.Web.Utils;

public static class BikeMediaEndpoint
{
    public const string CacheControl = "private, max-age=31536000, immutable";

    public static string PicturePath(string bikeId, DateTime savedAt) =>
        string.Create(CultureInfo.InvariantCulture, $"bikes/{Uri.EscapeDataString(bikeId)}/picture?v={savedAt.Ticks}");

    public static string DocumentPath(string bikeId, BikeDocumentDto document) =>
        string.Create(CultureInfo.InvariantCulture, $"bikes/{Uri.EscapeDataString(bikeId)}/documents/{Uri.EscapeDataString(document.FileId)}?v={document.SavedAt.Ticks}");
}
