using System.Net;
using System.Text;
using System.Web;
using EBikeManager.Application.Exceptions;
using EBikeManager.Application.Models;
using EBikeManager.Application.Services;
using EBikeManager.Application.Utils;
using EBikeManager.TestSupport;
using Microsoft.Extensions.Time.Testing;

namespace EBikeManager.UnitTests.Application.Services;

public class BoschAuthServiceTests
{
    private static readonly DateTime _now = new(2026, 9, 30, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void TheLoginUrlUsesTheFlowAppClientWithPkce()
    {
        var login = new PkceLoginService(new FakeTimeProvider()).Start(BoschAccountService.Provider);
        var service = new BoschAuthService(new StubHttpClientFactory(new RecordingHandler(_ => throw new InvalidOperationException())), TimeProvider.System);

        var url = new Uri(service.BuildAuthorizeUrl(login));
        var query = HttpUtility.ParseQueryString(url.Query);

        Assert.Equal(BoschEndpoints.AuthorizeUrl, url.GetLeftPart(UriPartial.Path));
        Assert.Equal("one-bike-app", query["client_id"]);
        Assert.Equal(BoschEndpoints.RedirectUri, query["redirect_uri"]);
        Assert.Equal("openid offline_access", query["scope"]);
        Assert.Equal(login.CodeChallenge, query["code_challenge"]);
        Assert.Equal("S256", query["code_challenge_method"]);
        Assert.Equal(login.State, query["state"]);
    }

    [Fact]
    public async Task ExchangingACodeReturnsTokensAndTheAccountName()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var handler = new RecordingHandler(_ => RecordingHandler.Json(TokenJson("access-1", "refresh-1", IdToken("rider@example.com"))));
        var time = new FakeTimeProvider(new DateTimeOffset(_now));
        var login = new PkceLoginService(time).Start(BoschAccountService.Provider);

        var tokens = await new BoschAuthService(new StubHttpClientFactory(handler), time).ExchangeCodeAsync("the-code", login, cancellationToken);

        Assert.Equal(new BoschTokens("access-1", "refresh-1", _now.AddSeconds(300), "rider@example.com"), tokens);
        var form = HttpUtility.ParseQueryString(Assert.Single(handler.Requests).Body);
        Assert.Equal("authorization_code", form["grant_type"]);
        Assert.Equal("the-code", form["code"]);
        Assert.Equal(login.CodeVerifier, form["code_verifier"]);
    }

    [Fact]
    public async Task InvalidGrantMeansSigningInAgain()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var handler = new RecordingHandler(_ => RecordingHandler.Json(
            """{"error":"invalid_grant","error_description":"Offline user session not found"}""", HttpStatusCode.BadRequest));

        var service = new BoschAuthService(new StubHttpClientFactory(handler), TimeProvider.System);

        var error = await Assert.ThrowsAsync<BoschReauthRequiredException>(() => service.RefreshAsync("old", cancellationToken));
        Assert.Contains("Offline user session not found", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ARefreshWithoutANewRefreshTokenKeepsTheOldOne()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var handler = new RecordingHandler(_ => RecordingHandler.Json(TokenJson("access-2", null)));

        var tokens = await new BoschAuthService(new StubHttpClientFactory(handler), TimeProvider.System).RefreshAsync("kept", cancellationToken);

        Assert.Equal("kept", tokens.RefreshToken);
    }

    [Fact]
    public async Task OtherFailuresAreHttpErrors()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.BadGateway));

        var service = new BoschAuthService(new StubHttpClientFactory(handler), TimeProvider.System);

        await Assert.ThrowsAsync<HttpRequestException>(() => service.RefreshAsync("old", cancellationToken));
    }

    internal static string TokenJson(string access, string? refresh, string? idToken = null) =>
        "{" + $"\"access_token\":\"{access}\",\"expires_in\":300"
            + (refresh == null ? "" : $",\"refresh_token\":\"{refresh}\"")
            + (idToken == null ? "" : $",\"id_token\":\"{idToken}\"") + "}";

    internal static string IdToken(string email)
    {
        static string Encode(string json) => Convert.ToBase64String(Encoding.UTF8.GetBytes(json)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        return $"{Encode("""{"alg":"RS256"}""")}.{Encode($$"""{"email":"{{email}}"}""")}.signature";
    }
}
