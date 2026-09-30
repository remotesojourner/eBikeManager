using System.Text.Json;
using EBikeManager.Application.Models;

namespace EBikeManager.Application.Utils;

public static class BoschJson
{
    private const double MillisecondThreshold = 1e12;

    public static IReadOnlyList<BoschBikeInfo> ParseBikes(JsonElement root)
    {
        var bikes = new List<BoschBikeInfo>();
        foreach (var item in Array(root, "data"))
        {
            if (Text(item, "id") is not { } id) continue;

            var attributes = Property(item, "attributes");
            var brand = Text(attributes, "brandName") ?? "eBike";
            var driveUnit = Text(Property(attributes, "driveUnit"), "productName");
            var frame = Text(attributes, "frameNumber");
            var name = driveUnit != null ? $"{brand} ({driveUnit})"
                : frame is { Length: >= 4 } ? $"{brand} (…{frame[^4..]})"
                : brand;
            bikes.Add(new BoschBikeInfo(id, name));
        }

        return bikes;
    }

    public static BoschActivityPage ParseActivityPage(JsonElement root)
    {
        var activities = new List<BoschActivity>();
        foreach (var item in Array(root, "data"))
        {
            var attributes = Property(item, "attributes");
            if (Text(item, "id") is not { } id || EpochTime(attributes, "startTime") is not { } start) continue;

            activities.Add(new BoschActivity(
                id,
                Text(attributes, "bikeId"),
                Text(attributes, "title"),
                start,
                EpochTime(attributes, "endTime"),
                Text(attributes, "timeZoneOfActivity"),
                Whole(attributes, "distance"),
                Whole(attributes, "durationWithoutStops"),
                Number(attributes, "caloriesBurnt"),
                Whole(attributes, "elevationGain"),
                Number(attributes, "averageSpeed"),
                Whole(attributes, "riderEnergyShare"),
                Number(attributes, "averageRiderPower"),
                attributes.ValueKind == JsonValueKind.Object ? attributes.GetRawText() : "{}"));
        }

        var pages = Property(root, "meta") is { ValueKind: JsonValueKind.Object } meta ? Whole(meta, "pages") ?? 0 : 0;
        return new BoschActivityPage(activities, pages);
    }

    private static DateTime? EpochTime(JsonElement element, string name) =>
        Number(element, name) switch
        {
            null => null,
            > MillisecondThreshold and var milliseconds => DateTime.UnixEpoch.AddMilliseconds(Math.Round(milliseconds)),
            var seconds => DateTime.UnixEpoch.AddSeconds(Math.Round(seconds.Value))
        };

    private static JsonElement Property(JsonElement element, string name) =>
        element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out var value) ? value : default;

    private static List<JsonElement> Array(JsonElement element, string name) =>
        Property(element, name) is { ValueKind: JsonValueKind.Array } array ? [.. array.EnumerateArray()] : [];

    private static string? Text(JsonElement element, string name) =>
        Property(element, name) is { ValueKind: JsonValueKind.String } value ? value.GetString() : null;

    private static double? Number(JsonElement element, string name) =>
        Property(element, name) is { ValueKind: JsonValueKind.Number } value ? value.GetDouble() : null;

    private static int? Whole(JsonElement element, string name) =>
        Number(element, name) is { } value ? (int)Math.Round(value) : null;
}
