using System.Net;
using System.Web;
using EBikeManager.Application.Exceptions;
using EBikeManager.Application.Models;
using EBikeManager.Application.Services;
using EBikeManager.Application.Utils;
using EBikeManager.TestSupport;
using Microsoft.Extensions.Time.Testing;

namespace EBikeManager.UnitTests.Application.Services;

public class GoogleHealthAuthServiceTests
{
    private static readonly DateTime _now = new(2026, 9, 30, 12, 0, 0, DateTimeKind.Utc);
    private const string Callback = "https://ebike.example.com/integrations/google-health/callback";

    private static readonly GoogleHealthClient _client = new(FakeGoogleHealthAuth.ClientId, "secret");

    [Fact]
    public void TheSignInUrlAsksForOfflineAccessToActivityDataWithPkce()
    {
        var login = new PkceLoginService(new FakeTimeProvider()).Start(GoogleHealthAccountService.Provider, Callback);
        var service = new GoogleHealthAuthService(new StubHttpClientFactory(new RecordingHandler(_ => throw new InvalidOperationException())), TimeProvider.System);

        var url = new Uri(service.BuildAuthorizeUrl(login, $" {FakeGoogleHealthAuth.ClientId} "));
        var query = HttpUtility.ParseQueryString(url.Query);

        Assert.Equal(GoogleHealthEndpoints.AuthorizeUrl, url.GetLeftPart(UriPartial.Path));
        Assert.Equal(FakeGoogleHealthAuth.ClientId, query["client_id"]);
        Assert.Equal(Callback, query["redirect_uri"]);
        Assert.Equal(["openid", "email", GoogleHealthEndpoints.WriteScope, GoogleHealthEndpoints.ReadScope], query["scope"]!.Split(' '));
        Assert.Equal(("offline", "consent"), (query["access_type"], query["prompt"]));
        Assert.Equal((login.CodeChallenge, "S256", login.State), (query["code_challenge"], query["code_challenge_method"], query["state"]));
    }

    [Fact]
    public async Task ExchangingACodeSendsTheClientAndReturnsTheGoogleAccount()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var handler = new RecordingHandler(_ => RecordingHandler.Json(BoschAuthServiceTests.TokenJson("access-1", "refresh-1", BoschAuthServiceTests.IdToken("rider@gmail.com"))));
        var time = new FakeTimeProvider(new DateTimeOffset(_now));
        var login = new PkceLoginService(time).Start(GoogleHealthAccountService.Provider, Callback);

        var tokens = await new GoogleHealthAuthService(new StubHttpClientFactory(handler), time).ExchangeCodeAsync("4/code", login, _client, cancellationToken);

        Assert.Equal(new GoogleHealthTokens("access-1", "refresh-1", _now.AddSeconds(300), "rider@gmail.com"), tokens);
        var request = Assert.Single(handler.Requests);
        Assert.Equal(GoogleHealthEndpoints.TokenUrl, request.Uri!.ToString());
        var form = HttpUtility.ParseQueryString(request.Body);
        Assert.Equal(("authorization_code", "4/code", login.CodeVerifier), (form["grant_type"], form["code"], form["code_verifier"]));
        Assert.Equal((FakeGoogleHealthAuth.ClientId, "secret"), (form["client_id"], form["client_secret"]));
        Assert.Equal(Callback, form["redirect_uri"]);
    }

    [Fact]
    public async Task ACodeWithoutALastingSignInIsRefused()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var handler = new RecordingHandler(_ => RecordingHandler.Json(BoschAuthServiceTests.TokenJson("access-1", null)));
        var login = new PkceLoginService(TimeProvider.System).Start(GoogleHealthAccountService.Provider, Callback);
        var service = new GoogleHealthAuthService(new StubHttpClientFactory(handler), TimeProvider.System);

        var error = await Assert.ThrowsAsync<HttpRequestException>(() => service.ExchangeCodeAsync("code", login, _client, cancellationToken));

        Assert.Contains("lasting sign-in", error.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("invalid_grant", "Token has been expired or revoked.", "Sign in to Google again")]
    [InlineData("invalid_client", "The OAuth client was not found.", "Check its client ID and secret")]
    public async Task RejectedSignInsNeedTheOwnerToSignInAgain(string code, string description, string advice)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var handler = new RecordingHandler(_ => RecordingHandler.Json($$"""{"error":"{{code}}","error_description":"{{description}}"}""", HttpStatusCode.BadRequest));
        var service = new GoogleHealthAuthService(new StubHttpClientFactory(handler), TimeProvider.System);

        var error = await Assert.ThrowsAsync<IntegrationSignInRequiredException>(() => service.RefreshAsync("old", _client, cancellationToken));

        Assert.Contains(description, error.Message, StringComparison.Ordinal);
        Assert.Contains(advice, error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ARefreshKeepsTheRefreshToken()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var handler = new RecordingHandler(_ => RecordingHandler.Json(BoschAuthServiceTests.TokenJson("access-2", null)));

        var tokens = await new GoogleHealthAuthService(new StubHttpClientFactory(handler), TimeProvider.System).RefreshAsync("kept", _client, cancellationToken);

        Assert.Equal(("access-2", "kept"), (tokens.AccessToken, tokens.RefreshToken));
        var form = HttpUtility.ParseQueryString(Assert.Single(handler.Requests).Body);
        Assert.Equal(("refresh_token", "kept", "secret"), (form["grant_type"], form["refresh_token"], form["client_secret"]));
    }
}
