using System.Text.Json;
using EBikeManager.Application.Extensions;
using EBikeManager.Application.Models;

namespace EBikeManager.Application.Utils;

public static class BoschJson
{
    private const double MillisecondThreshold = 1e12;

    public static IReadOnlyList<BoschBikeInfo> ParseBikes(JsonElement root)
    {
        var bikes = new List<BoschBikeInfo>();
        foreach (var item in root.Items("data"))
        {
            if (item.Text("id") is not { } id) continue;
            bikes.Add(new BoschBikeInfo(id, BikeName(item.Property("attributes"))));
        }

        return bikes;
    }

    public static string BikeName(JsonElement profile)
    {
        var brand = profile.Text("brandName") ?? "eBike";
        var driveUnit = profile.Property("driveUnit").Text("productName");
        var frame = profile.Text("frameNumber");
        return driveUnit != null ? $"{brand} ({driveUnit})"
            : frame is { Length: >= 4 } ? $"{brand} (…{frame[^4..]})"
            : brand;
    }

    public static BoschActivityPage ParseActivityPage(JsonElement root)
    {
        var activities = new List<BoschActivity>();
        foreach (var item in root.Items("data"))
        {
            var attributes = item.Property("attributes");
            if (item.Text("id") is not { } id || EpochTime(attributes, "startTime") is not { } start) continue;

            activities.Add(new BoschActivity(
                id,
                attributes.Text("bikeId"),
                attributes.Text("title"),
                start,
                EpochTime(attributes, "endTime"),
                attributes.Text("timeZoneOfActivity"),
                attributes.Whole("distance"),
                attributes.Whole("durationWithoutStops"),
                attributes.Number("caloriesBurnt"),
                attributes.Whole("elevationGain"),
                attributes.Number("averageSpeed"),
                attributes.Whole("riderEnergyShare"),
                attributes.Number("averageRiderPower"),
                attributes.ValueKind == JsonValueKind.Object ? attributes.GetRawText() : "{}"));
        }

        var pages = root.Property("meta") is { ValueKind: JsonValueKind.Object } meta ? meta.Whole("pages") ?? 0 : 0;
        return new BoschActivityPage(activities, pages);
    }

    public static IReadOnlyDictionary<string, AssistModeName> AssistModeNames(IEnumerable<string> rideSummaries)
    {
        var names = new Dictionary<string, AssistModeName>();
        foreach (var summary in rideSummaries)
        {
            try
            {
                using var json = JsonDocument.Parse(summary);
                foreach (var usage in json.RootElement.Items("assistModeUsage"))
                {
                    if (usage.Text("assistModeConfigId") is { } id && usage.Text("name") is { } name && !names.ContainsKey(id))
                    {
                        names[id] = new AssistModeName(name, Colour(usage.Number("color")));
                    }
                }
            }
            catch (JsonException)
            {
                continue;
            }
        }

        return names;
    }

    public static string? Colour(double? argb) =>
        argb is { } value && value >= 0 && value <= uint.MaxValue ? $"#{(uint)value & 0xFFFFFF:X6}" : null;

    public static Uri? PictureUrl(JsonElement profile)
    {
        var media = profile.Property("mediaAssets");
        return (media.Text("bikePictureUrl") ?? media.Text("bike_picture_url")) is { } text
            && Uri.TryCreate(text, UriKind.Absolute, out var address)
            && address.Scheme == Uri.UriSchemeHttps
                ? address
                : null;
    }

    public static string? UnwrapProfile(JsonElement root) =>
        root.Property("data").Property("attributes") is { ValueKind: JsonValueKind.Object } attributes ? attributes.GetRawText()
        : root.ValueKind == JsonValueKind.Object ? root.GetRawText()
        : null;

    public static string? PassFor(JsonElement root, string bikeId) =>
        root.Items("bikePasses").FirstOrDefault(pass => pass.Text("bikeId") == bikeId) is { ValueKind: JsonValueKind.Object } pass ? pass.GetRawText() : null;

    public static string? LatestLocation(JsonElement root) =>
        root.Items("locations") is [var latest, ..] && latest.ValueKind == JsonValueKind.Object ? latest.GetRawText() : null;

    private static DateTime? EpochTime(JsonElement element, string name) =>
        element.Number(name) switch
        {
            null => null,
            > MillisecondThreshold and var milliseconds => DateTime.UnixEpoch.AddMilliseconds(Math.Round(milliseconds)),
            var seconds => DateTime.UnixEpoch.AddSeconds(Math.Round(seconds.Value))
        };
}
