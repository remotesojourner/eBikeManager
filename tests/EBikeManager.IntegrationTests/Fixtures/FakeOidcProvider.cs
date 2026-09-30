using System.Collections.Concurrent;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using EBikeManager.Application.Services.Interfaces;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Primitives;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace EBikeManager.IntegrationTests.Fixtures;

public sealed class FakeOidcProvider : HttpMessageHandler, IOidcDiscoveryService
{
    public const string Authority = "https://id.example.test/application/o/ebike-manager/";
    public const string AuthorizePath = "/test-oidc/authorize";
    public const string ClientId = "ebike-manager";
    public const string ClientSecret = "oidc-client-secret";
    public const string UserName = "Rider Example";

    private const string Subject = "rider-1";
    private const string TokenUrl = "https://id.example.test/token";
    private const string KeysUrl = "https://id.example.test/keys";
    private const string UserInfoUrl = "https://id.example.test/userinfo";

    private readonly RSA _rsa = RSA.Create(2048);
    private readonly ConcurrentDictionary<string, Grant> _grants = new();
    private readonly ConcurrentQueue<Uri> _authorizations = new();

    private sealed record Grant(string Nonce, string CodeChallenge, string RedirectUri);

    public IReadOnlyCollection<Uri> Authorizations => _authorizations;

    public Uri AuthorizeEndpoint { get; set; } = new("http://localhost" + AuthorizePath);

    public Uri Approve(Uri authorizeRequest)
    {
        _authorizations.Enqueue(authorizeRequest);
        var query = QueryHelpers.ParseQuery(authorizeRequest.Query);
        Assert.Equal(ClientId, query["client_id"].ToString());
        Assert.Equal("code", query["response_type"].ToString());
        Assert.Equal("S256", query["code_challenge_method"].ToString());

        var code = Convert.ToHexString(RandomNumberGenerator.GetBytes(16));
        var redirectUri = query["redirect_uri"].ToString();
        _grants[code] = new Grant(query["nonce"].ToString(), query["code_challenge"].ToString(), redirectUri);
        return new Uri(QueryHelpers.AddQueryString(redirectUri, new Dictionary<string, string?> { ["code"] = code, ["state"] = query["state"].ToString() }));
    }

    public Task<string?> FindProblemAsync(string authority, CancellationToken cancellationToken = default) =>
        Task.FromResult(authority.TrimEnd('/') == Authority.TrimEnd('/') ? null : $"No provider answers at {authority}.");

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var address = request.RequestUri!.GetLeftPart(UriPartial.Path);
        if (address == Authority + ".well-known/openid-configuration") return Json(Discovery());
        if (address == KeysUrl) return Json(Keys());
        if (address == UserInfoUrl) return Json(new { sub = Subject, name = UserName, email = "rider@example.test" });
        if (address == TokenUrl && request.Content != null) return Token(QueryHelpers.ParseQuery(await request.Content.ReadAsStringAsync(cancellationToken)));
        return new HttpResponseMessage(HttpStatusCode.NotFound);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _rsa.Dispose();
        base.Dispose(disposing);
    }

    private Dictionary<string, object> Discovery() => new()
    {
        ["issuer"] = Authority,
        ["authorization_endpoint"] = AuthorizeEndpoint.AbsoluteUri,
        ["token_endpoint"] = TokenUrl,
        ["userinfo_endpoint"] = UserInfoUrl,
        ["jwks_uri"] = KeysUrl,
        ["response_types_supported"] = new[] { "code" },
        ["subject_types_supported"] = new[] { "public" },
        ["id_token_signing_alg_values_supported"] = new[] { SecurityAlgorithms.RsaSha256 }
    };

    private object Keys()
    {
        var key = _rsa.ExportParameters(includePrivateParameters: false);
        return new
        {
            keys = new[]
            {
                new { kty = "RSA", use = "sig", alg = SecurityAlgorithms.RsaSha256, kid = "test-key", n = Base64UrlEncoder.Encode(key.Modulus), e = Base64UrlEncoder.Encode(key.Exponent) }
            }
        };
    }

    private HttpResponseMessage Token(Dictionary<string, StringValues> form)
    {
        if (!_grants.TryRemove(form["code"].ToString(), out var grant)
            || form["client_id"] != ClientId
            || form["client_secret"] != ClientSecret
            || form["redirect_uri"] != grant.RedirectUri
            || Challenge(form["code_verifier"].ToString()) != grant.CodeChallenge)
        {
            return new HttpResponseMessage(HttpStatusCode.BadRequest) { Content = new StringContent("""{ "error": "invalid_grant" }""", Encoding.UTF8, "application/json") };
        }

        var accessToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(16));
        var now = DateTime.UtcNow;
        var idToken = new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = Authority,
            Audience = ClientId,
            IssuedAt = now,
            NotBefore = now,
            Expires = now.AddMinutes(5),
            Claims = new Dictionary<string, object>
            {
                ["sub"] = Subject,
                ["name"] = UserName,
                ["nonce"] = grant.Nonce,
                ["at_hash"] = Base64UrlEncoder.Encode(SHA256.HashData(Encoding.ASCII.GetBytes(accessToken))[..16])
            },
            SigningCredentials = new SigningCredentials(new RsaSecurityKey(_rsa) { KeyId = "test-key" }, SecurityAlgorithms.RsaSha256)
        });

        return Json(new { access_token = accessToken, token_type = "Bearer", expires_in = 300, id_token = idToken });
    }

    private static string Challenge(string verifier) => Base64UrlEncoder.Encode(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)));

    private static HttpResponseMessage Json(object body) =>
        new(HttpStatusCode.OK) { Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json") };
}
