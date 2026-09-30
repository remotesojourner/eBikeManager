using System.Text;
using System.Text.Json;

namespace EBikeManager.Application.Utils;

public static class IdTokens
{
    private static readonly string[] _accountClaims = ["email", "preferred_username", "name"];

    public static string? AccountName(string? idToken)
    {
        var parts = idToken?.Split('.');
        if (parts is not { Length: 3 }) return null;

        try
        {
            var payload = parts[1].Replace('-', '+').Replace('_', '/');
            payload = payload.PadRight(payload.Length + ((4 - (payload.Length % 4)) % 4), '=');
            using var claims = JsonDocument.Parse(Encoding.UTF8.GetString(Convert.FromBase64String(payload)));
            return _accountClaims
                .Select(claim => claims.RootElement.TryGetProperty(claim, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null)
                .FirstOrDefault(value => value != null);
        }
        catch (Exception ex) when (ex is FormatException or JsonException)
        {
            return null;
        }
    }
}
