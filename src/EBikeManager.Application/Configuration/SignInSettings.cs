namespace EBikeManager.Application.Configuration;

public sealed record SignInSettings(bool Enabled, string? Authority, string? ClientId, string Scopes, string? Stamp)
{
    public const string DefaultScopes = "openid profile email";

    private const string OpenIdScope = "openid";

    public bool Configured => Authority != null && ClientId != null;

    public bool IsActive => Enabled && Configured;

    public IReadOnlyList<string> ScopeList => ParseScopes(Scopes);

    public static bool IsValidAuthority(string? authority) =>
        Uri.TryCreate(authority?.Trim(), UriKind.Absolute, out var address)
        && (address.Scheme == Uri.UriSchemeHttps || address.Scheme == Uri.UriSchemeHttp)
        && string.IsNullOrEmpty(address.Query)
        && string.IsNullOrEmpty(address.Fragment);

    public static IReadOnlyList<string> ParseScopes(string? scopes)
    {
        var parsed = (scopes ?? "").Split([' ', ','], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(scope => scope != OpenIdScope)
            .Distinct(StringComparer.Ordinal);
        return [OpenIdScope, .. parsed];
    }
}
