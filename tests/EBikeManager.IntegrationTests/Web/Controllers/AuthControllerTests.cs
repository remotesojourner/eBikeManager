using System.Net;
using EBikeManager.Application.Configuration;
using EBikeManager.Application.Repositories.Interfaces;
using EBikeManager.IntegrationTests.Fixtures;
using EBikeManager.Web.Services;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;

namespace EBikeManager.IntegrationTests.Web.Controllers;

public sealed class AuthControllerTests : IClassFixture<SignInRequiredApp>, IClassFixture<SetUpApp>
{
    private static readonly Uri _rides = new("/rides", UriKind.Relative);

    private readonly SignInRequiredApp _app;
    private readonly SetUpApp _openApp;

    public AuthControllerTests(SignInRequiredApp app, SetUpApp openApp)
    {
        _app = app;
        _openApp = openApp;
    }

    [Fact]
    public async Task EveryPageSendsPeopleWhoAreNotSignedInStraightToTheProvider()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var client = Client(_app);

        using var page = await client.GetAsync(_rides, cancellationToken);
        Assert.Equal(HttpStatusCode.Redirect, page.StatusCode);
        Assert.Equal("/auth/login?returnUrl=%2Frides", page.Headers.Location?.OriginalString);

        using var login = await client.GetAsync(page.Headers.Location, cancellationToken);
        Assert.Equal(HttpStatusCode.Redirect, login.StatusCode);
        var authorize = login.Headers.Location!;
        Assert.Equal(_app.Oidc.AuthorizeEndpoint.AbsoluteUri, authorize.GetLeftPart(UriPartial.Path));
        var query = QueryHelpers.ParseQuery(authorize.Query);
        Assert.Equal(FakeOidcProvider.ClientId, query["client_id"].ToString());
        Assert.Equal("http://localhost/signin-oidc", query["redirect_uri"].ToString());
        Assert.Equal("openid profile email", query["scope"].ToString());
    }

    [Fact]
    public async Task SigningInWithTheProviderOpensTheAppAndItsMedia()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var client = await SignInAsync(cancellationToken);

        using var page = await client.GetAsync(_rides, cancellationToken);
        using var picture = await client.GetAsync(new Uri($"/bikes/{SampleData.BikeId}/picture", UriKind.Relative), cancellationToken);

        Assert.Equal(HttpStatusCode.OK, page.StatusCode);
        Assert.Equal(HttpStatusCode.OK, picture.StatusCode);
    }

    [Fact]
    public async Task TheLiveConnectionAndMediaAnswerUnauthorizedWithoutSigningIn()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var client = Client(_app);

        using var hub = await client.PostAsync(new Uri("/_blazor/negotiate?negotiateVersion=1", UriKind.Relative), null, cancellationToken);
        using var picture = await client.GetAsync(new Uri($"/bikes/{SampleData.BikeId}/picture", UriKind.Relative), cancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, hub.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, picture.StatusCode);
    }

    [Fact]
    public async Task ChangingTheProviderSignsEveryoneOut()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var client = await SignInAsync(cancellationToken);

        using (var scope = _app.Services.CreateScope())
        {
            await scope.ServiceProvider.GetRequiredService<ISettingsRepository>()
                .SaveSignInAsync(new Dictionary<string, string> { [SettingDefinitions.AuthStamp] = Guid.NewGuid().ToString("N") }, cancellationToken);
        }

        await _app.Services.GetRequiredService<AuthSettingsService>().ReloadAsync(cancellationToken);

        using var page = await client.GetAsync(_rides, cancellationToken);
        Assert.Equal(HttpStatusCode.Redirect, page.StatusCode);
        Assert.StartsWith("/auth/login", page.Headers.Location?.OriginalString, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SigningOutNeedsTheTokenFromTheSignOutForm()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var client = await SignInAsync(cancellationToken);

        using var forged = await client.PostAsync(new Uri("/auth/logout", UriKind.Relative), new FormUrlEncodedContent([]), cancellationToken);
        using var page = await client.GetAsync(_rides, cancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, forged.StatusCode);
        Assert.Equal(HttpStatusCode.OK, page.StatusCode);
    }

    [Fact]
    public async Task ARefusedSignInExplainsWhatHappenedAndHowToRecover()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var client = Client(_app);

        using var callback = await client.GetAsync(new Uri("/signin-oidc?error=access_denied&state=unknown", UriKind.Relative), cancellationToken);
        Assert.Equal(HttpStatusCode.Redirect, callback.StatusCode);
        Assert.StartsWith("/auth/failed?reason=", callback.Headers.Location?.OriginalString, StringComparison.Ordinal);

        using var failed = await client.GetAsync(callback.Headers.Location, cancellationToken);
        var html = await failed.Content.ReadAsStringAsync(cancellationToken);
        Assert.Equal(HttpStatusCode.OK, failed.StatusCode);
        Assert.Contains("Signing in didn&#x27;t work", html, StringComparison.Ordinal);
        Assert.Contains("DISABLE_AUTH=true", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task WithoutSignInThePagesOpenAndSignInGoesStraightBack()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var client = Client(_openApp);

        using var page = await client.GetAsync(_rides, cancellationToken);
        using var login = await client.GetAsync(new Uri("/auth/login?returnUrl=%2Frides", UriKind.Relative), cancellationToken);

        Assert.Equal(HttpStatusCode.OK, page.StatusCode);
        Assert.Equal(HttpStatusCode.Redirect, login.StatusCode);
        Assert.Equal("/rides", login.Headers.Location?.OriginalString);
    }

    private Task<HttpClient> SignInAsync(CancellationToken cancellationToken) => _app.CreateSignedInClientAsync(cancellationToken);

    private static HttpClient Client(TestApp app) => app.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
}
