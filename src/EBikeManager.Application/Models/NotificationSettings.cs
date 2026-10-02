using System.Globalization;
using System.Text.Json;

namespace EBikeManager.Application.Models;

public sealed class NotificationSettings
{
    private readonly IReadOnlyDictionary<string, JsonElement> _values;

    private NotificationSettings(IReadOnlyDictionary<string, JsonElement> values)
    {
        _values = values;
    }

    public static NotificationSettings Empty { get; } = new(new Dictionary<string, JsonElement>());

    public static NotificationSettings? Parse(string json)
    {
        try
        {
            var values = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
            return values == null ? Empty : new NotificationSettings(values);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public string GetString(string key, string fallback = "")
    {
        if (!_values.TryGetValue(key, out var value)) return fallback;

        return value.ValueKind switch
        {
            JsonValueKind.Null or JsonValueKind.Undefined => fallback,
            JsonValueKind.String => value.GetString() is { } text && !string.IsNullOrWhiteSpace(text) ? text : fallback,
            _ => value.GetRawText()
        };
    }

    public bool GetBool(string key, bool fallback = false)
    {
        if (!_values.TryGetValue(key, out var value)) return fallback;

        return value.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.String when bool.TryParse(value.GetString(), out var parsed) => parsed,
            _ => fallback
        };
    }

    public int GetInt(string key, int fallback)
    {
        if (!_values.TryGetValue(key, out var value)) return fallback;

        return value.ValueKind switch
        {
            JsonValueKind.Number when value.TryGetInt32(out var number) => number,
            JsonValueKind.String when int.TryParse(value.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) => parsed,
            _ => fallback
        };
    }
}
