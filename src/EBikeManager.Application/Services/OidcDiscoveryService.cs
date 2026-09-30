using System.Text.Json;
using EBikeManager.Application.Extensions;
using EBikeManager.Application.Resources;
using EBikeManager.Application.Services.Interfaces;

namespace EBikeManager.Application.Services;

internal sealed class OidcDiscoveryService : IOidcDiscoveryService
{
    public const string HttpClientName = "oidc-discovery";

    private readonly IHttpClientFactory _httpClientFactory;

    public OidcDiscoveryService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public static Uri DocumentFor(string authority) => new(authority.TrimEnd('/') + "/.well-known/openid-configuration");

    public async Task<string?> FindProblemAsync(string authority, CancellationToken cancellationToken = default)
    {
        var address = DocumentFor(authority);
        try
        {
            using var http = _httpClientFactory.CreateClient(HttpClientName);
            using var response = await http.GetAsync(address, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return ApplicationStrings.Format(ApplicationStrings.OidcProviderAnswered, (int)response.StatusCode, address);

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            var root = document.RootElement;
            return root.Text("authorization_endpoint") == null || root.Text("token_endpoint") == null
                ? ApplicationStrings.Format(ApplicationStrings.OidcNotDiscoveryDocument, address)
                : null;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            return ApplicationStrings.Format(ApplicationStrings.OidcUnreadable, address, ex.Message);
        }
    }
}
