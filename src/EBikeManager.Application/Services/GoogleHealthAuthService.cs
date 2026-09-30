using System.Net;
using System.Text.Json;
using EBikeManager.Application.Exceptions;
using EBikeManager.Application.Models;
using EBikeManager.Application.Resources;
using EBikeManager.Application.Services.Interfaces;
using EBikeManager.Application.Utils;

namespace EBikeManager.Application.Services;

internal sealed class GoogleHealthAuthService : IGoogleHealthAuthService
{
    public const string HttpClientName = "google-auth";

    private const int DefaultLifetimeSeconds = 3600;

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly TimeProvider _time;

    public GoogleHealthAuthService(IHttpClientFactory httpClientFactory, TimeProvider time)
    {
        _httpClientFactory = httpClientFactory;
        _time = time;
    }

    public string BuildAuthorizeUrl(PendingLogin login, string clientId)
    {
        var query = new Dictionary<string, string>
        {
            ["client_id"] = clientId.Trim(),
            ["redirect_uri"] = RedirectUriOf(login),
            ["response_type"] = "code",
            ["scope"] = GoogleHealthEndpoints.Scope,
            ["code_challenge"] = login.CodeChallenge,
            ["code_challenge_method"] = "S256",
            ["access_type"] = "offline",
            ["prompt"] = "consent",
            ["state"] = login.State
        };
        return GoogleHealthEndpoints.AuthorizeUrl + "?" + string.Join('&', query.Select(pair => $"{pair.Key}={Uri.EscapeDataString(pair.Value)}"));
    }

    public Task<GoogleHealthTokens> ExchangeCodeAsync(string code, PendingLogin login, GoogleHealthClient client, CancellationToken cancellationToken = default) =>
        RequestTokensAsync(new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["client_id"] = client.ClientId,
            ["client_secret"] = client.ClientSecret,
            ["code"] = code,
            ["code_verifier"] = login.CodeVerifier,
            ["redirect_uri"] = RedirectUriOf(login)
        }, cancellationToken);

    public Task<GoogleHealthTokens> RefreshAsync(string refreshToken, GoogleHealthClient client, CancellationToken cancellationToken = default) =>
        RequestTokensAsync(new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["client_id"] = client.ClientId,
            ["client_secret"] = client.ClientSecret,
            ["refresh_token"] = refreshToken
        }, cancellationToken);

    public async Task RevokeAsync(string token, CancellationToken cancellationToken = default)
    {
        using var http = _httpClientFactory.CreateClient(HttpClientName);
        using var content = new FormUrlEncodedContent(new Dictionary<string, string> { ["token"] = token });
        using var response = await http.PostAsync(new Uri(GoogleHealthEndpoints.RevokeUrl), content, cancellationToken);
    }

    private async Task<GoogleHealthTokens> RequestTokensAsync(Dictionary<string, string> form, CancellationToken cancellationToken)
    {
        using var http = _httpClientFactory.CreateClient(HttpClientName);
        using var content = new FormUrlEncodedContent(form);
        using var response = await http.PostAsync(new Uri(GoogleHealthEndpoints.TokenUrl), content, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Unauthorized && ReadObject(body) is { } error)
        {
            using (error)
            {
                var code = Text(error.RootElement, "error");
                var description = Text(error.RootElement, "error_description") ?? code ?? "";
                if (code == "invalid_grant") throw new IntegrationSignInRequiredException(ApplicationStrings.Format(ApplicationStrings.GoogleHealthRejectedLogin, description));
                if (code is "invalid_client" or "unauthorized_client") throw new IntegrationSignInRequiredException(ApplicationStrings.Format(ApplicationStrings.GoogleHealthClientRejected, description));
            }
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                ApplicationStrings.Format(ApplicationStrings.GoogleHealthTokenFailed, (int)response.StatusCode, Shorten(body)), null, response.StatusCode);
        }

        using var tokens = ReadObject(body) ?? throw new HttpRequestException(ApplicationStrings.GoogleHealthTokenUnreadable);
        var root = tokens.RootElement;
        var accessToken = Text(root, "access_token") ?? throw new HttpRequestException(ApplicationStrings.GoogleHealthTokenUnreadable);
        var refreshToken = Text(root, "refresh_token") ?? form.GetValueOrDefault("refresh_token")
            ?? throw new HttpRequestException(ApplicationStrings.GoogleHealthNoRefreshToken);
        var lifetime = root.TryGetProperty("expires_in", out var expires) && expires.TryGetInt32(out var seconds) ? seconds : DefaultLifetimeSeconds;

        return new GoogleHealthTokens(accessToken, refreshToken, _time.GetUtcNow().UtcDateTime.AddSeconds(lifetime), IdTokens.AccountName(Text(root, "id_token")));
    }

    private static string RedirectUriOf(PendingLogin login) =>
        login.RedirectUri ?? throw new InvalidOperationException("A Google sign-in needs the address Google sends the browser back to.");

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
