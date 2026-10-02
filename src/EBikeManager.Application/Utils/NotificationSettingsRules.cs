using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using EBikeManager.Application.Models.Dtos;
using EBikeManager.Application.Resources;

namespace EBikeManager.Application.Utils;

public static class NotificationSettingsRules
{
    public const int MaxTextLength = 255;
    public const int MaxTextareaLength = 2000;

    private static readonly TimeSpan _regexTimeout = TimeSpan.FromMilliseconds(200);

    public static string? ProblemWith(NotificationTypeSchemaDto schema, IReadOnlyDictionary<string, JsonElement> settings, IEnumerable<string> changedKeys)
    {
        var fieldNames = schema.Fields.Select(field => field.Name).ToHashSet(StringComparer.Ordinal);
        if (changedKeys.FirstOrDefault(key => !fieldNames.Contains(key)) is { } unknown)
            return ApplicationStrings.Format(ApplicationStrings.NotificationUnknownSetting, unknown, schema.Title);

        return schema.Fields
            .Select(field => ProblemWith(field, settings.TryGetValue(field.Name, out var value) ? value : default))
            .FirstOrDefault(problem => problem != null);
    }

    public static bool MatchesPattern(string? pattern, string text)
    {
        if (string.IsNullOrEmpty(pattern)) return true;

        try
        {
            return Regex.IsMatch(text, pattern, RegexOptions.None, _regexTimeout);
        }
        catch (Exception ex) when (ex is ArgumentException or RegexMatchTimeoutException)
        {
            return true;
        }
    }

    private static string? ProblemWith(NotificationFieldSchemaDto field, JsonElement value)
    {
        if (value.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
            return field.Required ? ApplicationStrings.Format(ApplicationStrings.NotificationFieldRequired, field.Name) : null;

        return field.Type switch
        {
            "boolean" => value.ValueKind is JsonValueKind.True or JsonValueKind.False ? null : ApplicationStrings.Format(ApplicationStrings.NotificationFieldBoolean, field.Name),
            "number" => IsWholeNumber(value) ? null : ApplicationStrings.Format(ApplicationStrings.NotificationFieldNumber, field.Name),
            _ => TextProblem(field, value)
        };
    }

    private static bool IsWholeNumber(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.Number => value.TryGetInt32(out _),
        JsonValueKind.String => int.TryParse(value.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out _),
        _ => false
    };

    private static string? TextProblem(NotificationFieldSchemaDto field, JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.String) return ApplicationStrings.Format(ApplicationStrings.NotificationFieldText, field.Name);

        var text = value.GetString()!;
        if (text.Length == 0) return field.Required ? ApplicationStrings.Format(ApplicationStrings.NotificationFieldRequired, field.Name) : null;

        var maxLength = field.Type == "textarea" ? MaxTextareaLength : MaxTextLength;
        if (text.Length > maxLength) return ApplicationStrings.Format(ApplicationStrings.NotificationFieldTooLong, field.Name, maxLength);

        return MatchesPattern(field.Regex, text) ? null : ApplicationStrings.Format(ApplicationStrings.NotificationFieldFormat, field.Name);
    }
}
