using EBikeManager.Application.Enums;

namespace EBikeManager.Application.Configuration;

public static class UnitSettings
{
    public const string MetricKey = "metric";
    public const string ImperialKey = "imperial";

    private static readonly string[] _imperialLanguages = ["en-US", "en-GB"];

    public static string KeyFor(UnitSystem units) => units == UnitSystem.Imperial ? ImperialKey : MetricKey;

    public static UnitSystem? SystemFor(string? key) => key switch
    {
        MetricKey => UnitSystem.Metric,
        ImperialKey => UnitSystem.Imperial,
        _ => null
    };

    public static UnitSystem SuggestFor(string? language) =>
        _imperialLanguages.Contains(language?.Trim(), StringComparer.OrdinalIgnoreCase) ? UnitSystem.Imperial : UnitSystem.Metric;
}
