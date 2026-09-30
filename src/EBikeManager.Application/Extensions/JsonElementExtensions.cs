using System.Text.Json;

namespace EBikeManager.Application.Extensions;

public static class JsonElementExtensions
{
    public static JsonElement Property(this JsonElement element, string name) =>
        element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out var value) ? value : default;

    public static List<JsonElement> Items(this JsonElement element, string name) =>
        element.Property(name) is { ValueKind: JsonValueKind.Array } array ? [.. array.EnumerateArray()] : [];

    public static string? Text(this JsonElement element, string name) =>
        element.Property(name) is { ValueKind: JsonValueKind.String } value && !string.IsNullOrWhiteSpace(value.GetString()) ? value.GetString() : null;

    public static double? Number(this JsonElement element, string name) =>
        element.Property(name) is { ValueKind: JsonValueKind.Number } value ? value.GetDouble() : null;

    public static int? Whole(this JsonElement element, string name) =>
        element.Number(name) is { } value ? (int)Math.Round(value) : null;

    public static bool? Flag(this JsonElement element, string name) =>
        element.Property(name).ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            _ => null
        };

    public static DateTime? Timestamp(this JsonElement element, string name) =>
        element.Text(name) is { } text && DateTimeOffset.TryParse(text, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AssumeUniversal, out var parsed)
            ? parsed.UtcDateTime
            : null;
}
