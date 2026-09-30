using System.Text.RegularExpressions;

namespace EBikeManager.Application.Models;

public sealed partial record OAuthCallback(string? Code, string? State, string? Error)
{
    public static OAuthCallback Parse(string? pasted)
    {
        var text = pasted?.Trim() ?? "";
        return new OAuthCallback(Find("code", text), Find("state", text), Find("error_description", text) ?? Find("error", text));
    }

    private static string? Find(string name, string text)
    {
        foreach (Match match in ParameterPattern().Matches(text))
        {
            if (match.Groups["name"].Value == name) return Uri.UnescapeDataString(match.Groups["value"].Value.Replace('+', ' '));
        }

        return null;
    }

    [GeneratedRegex(@"(?:^|[?&#])(?<name>[A-Za-z_]+)=(?<value>[^&#\s]*)")]
    private static partial Regex ParameterPattern();
}
