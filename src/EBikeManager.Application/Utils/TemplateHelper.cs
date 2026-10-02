using System.Globalization;

namespace EBikeManager.Application.Utils;

public static class TemplateHelper
{
    public static string ReplaceVariables(string template, IReadOnlyDictionary<string, string> variables, DateTime localNow)
    {
        if (string.IsNullOrEmpty(template)) return string.Empty;

        var all = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["year"] = localNow.Year.ToString(CultureInfo.InvariantCulture),
            ["month"] = localNow.Month.ToString("D2", CultureInfo.InvariantCulture),
            ["day"] = localNow.Day.ToString("D2", CultureInfo.InvariantCulture),
            ["hour"] = localNow.Hour.ToString("D2", CultureInfo.InvariantCulture),
            ["minute"] = localNow.Minute.ToString("D2", CultureInfo.InvariantCulture),
            ["second"] = localNow.Second.ToString("D2", CultureInfo.InvariantCulture)
        };
        foreach (var (name, value) in variables) all[name] = value;

        var result = template;
        foreach (var (name, value) in all) result = result.Replace($"%{name}%", value, StringComparison.OrdinalIgnoreCase);
        return result;
    }
}
