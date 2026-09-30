using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using EBikeManager.Application.Exceptions;
using EBikeManager.Application.Models;
using EBikeManager.Application.Resources;
using EBikeManager.Application.Services.Interfaces;
using EBikeManager.Application.Utils;

namespace EBikeManager.Application.Services;

internal sealed class BoschAuthService : IBoschAuthService
{
    public const string HttpClientName = "bosch-auth";

    private const int DefaultLifetimeSeconds = 300;

    private static readonly string[] _accountClaims = ["email", "preferred_username", "name"];

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly TimeProvider _time;

    public BoschAuthService(IHttpClientFactory httpClientFactory, TimeProvider time)
    {
        _httpClientFactory = httpClientFactory;
        _time = time;
    }

    public string BuildAuthorizeUrl(PendingLogin login)
    {
        var query = new Dictionary<string, string>
        {
            ["client_id"] = BoschEndpoints.ClientId,
            ["redirect_uri"] = BoschEndpoints.RedirectUri,
            ["response_type"] = "code",
            ["scope"] = BoschEndpoints.Scope,
            ["code_challenge"] = login.CodeChallenge,
            ["code_challenge_method"] = "S256",
            ["kc_idp_hint"] = "skid",
            ["prompt"] = "login",
            ["state"] = login.State,
            ["nonce"] = Convert.ToHexString(RandomNumberGenerator.GetBytes(16))
        };
        return BoschEndpoints.AuthorizeUrl + "?" + string.Join('&', query.Select(pair => $"{pair.Key}={Uri.EscapeDataString(pair.Value)}"));
    }

    public Task<BoschTokens> ExchangeCodeAsync(string code, PendingLogin login, CancellationToken cancellationToken = default) =>
        RequestTokensAsync(new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["client_id"] = BoschEndpoints.ClientId,
            ["code"] = code,
            ["code_verifier"] = login.CodeVerifier,
            ["redirect_uri"] = BoschEndpoints.RedirectUri
        }, cancellationToken);

    public Task<BoschTokens> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default) =>
        RequestTokensAsync(new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["client_id"] = BoschEndpoints.ClientId,
            ["refresh_token"] = refreshToken
        }, cancellationToken);

    private async Task<BoschTokens> RequestTokensAsync(Dictionary<string, string> form, CancellationToken cancellationToken)
    {
        using var http = _httpClientFactory.CreateClient(HttpClientName);
        using var content = new FormUrlEncodedContent(form);
        using var response = await http.PostAsync(new Uri(BoschEndpoints.TokenUrl), content, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Unauthorized
            && ReadObject(body) is { } error
            && Text(error.RootElement, "error") == "invalid_grant")
        {
            using (error)
            {
                throw new BoschReauthRequiredException(ApplicationStrings.Format(
                    ApplicationStrings.BoschRejectedLogin, Text(error.RootElement, "error_description") ?? "invalid_grant"));
            }
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                ApplicationStrings.Format(ApplicationStrings.BoschTokenFailed, (int)response.StatusCode, Shorten(body)), null, response.StatusCode);
        }

        using var tokens = ReadObject(body) ?? throw new HttpRequestException(ApplicationStrings.BoschTokenUnreadable);
        var root = tokens.RootElement;
        var accessToken = Text(root, "access_token") ?? throw new HttpRequestException(ApplicationStrings.BoschTokenUnreadable);
        var refreshToken = Text(root, "refresh_token") ?? form.GetValueOrDefault("refresh_token")
            ?? throw new HttpRequestException(ApplicationStrings.BoschTokenUnreadable);
        var lifetime = root.TryGetProperty("expires_in", out var expires) && expires.TryGetInt32(out var seconds) ? seconds : DefaultLifetimeSeconds;

        return new BoschTokens(accessToken, refreshToken, _time.GetUtcNow().UtcDateTime.AddSeconds(lifetime), AccountNameFrom(Text(root, "id_token")));
    }

    internal static string? AccountNameFrom(string? idToken)
    {
        var parts = idToken?.Split('.');
        if (parts is not { Length: 3 }) return null;

        try
        {
            var payload = parts[1].Replace('-', '+').Replace('_', '/');
            payload = payload.PadRight(payload.Length + ((4 - (payload.Length % 4)) % 4), '=');
            using var claims = JsonDocument.Parse(Encoding.UTF8.GetString(Convert.FromBase64String(payload)));
            return _accountClaims.Select(claim => Text(claims.RootElement, claim)).FirstOrDefault(value => value != null);
        }
        catch (Exception ex) when (ex is FormatException or JsonException)
        {
            return null;
        }
    }

    private static JsonDocument? ReadObject(string json)
    {
        try
        {
            var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind == JsonValueKind.Object) return document;

            document.Dispose();
            return null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string? Text(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    private static string Shorten(string text) => text.Length > 300 ? text[..300] + "…" : text;
}
