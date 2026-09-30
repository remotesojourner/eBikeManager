using System.Net;
using EBikeManager.Application.Repositories.Interfaces;
using EBikeManager.IntegrationTests.Fixtures;
using EBikeManager.Web.Services;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace EBikeManager.IntegrationTests.Web.Controllers;

public sealed class SignInSwitchedOffTests : IClassFixture<SignInSwitchedOffApp>
{
    private readonly SignInSwitchedOffApp _app;

    public SignInSwitchedOffTests(SignInSwitchedOffApp app)
    {
        _app = app;
    }

    [Fact]
    public async Task DisableAuthOpensEverythingWhateverTheSavedSignInSettingsSay()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var client = _app.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        using var page = await client.GetAsync(new Uri("/rides", UriKind.Relative), cancellationToken);
        using var picture = await client.GetAsync(new Uri($"/bikes/{SampleData.BikeId}/picture", UriKind.Relative), cancellationToken);
        using var login = await client.GetAsync(new Uri("/auth/login?returnUrl=%2Fsettings%2Fsecurity", UriKind.Relative), cancellationToken);

        Assert.Equal(HttpStatusCode.OK, page.StatusCode);
        Assert.Equal(HttpStatusCode.OK, picture.StatusCode);
        Assert.Equal(HttpStatusCode.Redirect, login.StatusCode);
        Assert.Equal("/settings/security", login.Headers.Location?.OriginalString);
        Assert.Empty(_app.Oidc.Authorizations);

        var auth = _app.Services.GetRequiredService<AuthSettingsService>();
        Assert.True(auth.DisabledByEnvironment);
        Assert.False(auth.IsActive);
        using var scope = _app.Services.CreateScope();
        Assert.True((await scope.ServiceProvider.GetRequiredService<ISettingsRepository>().GetAsync(cancellationToken)).SignIn.IsActive);
    }
}
