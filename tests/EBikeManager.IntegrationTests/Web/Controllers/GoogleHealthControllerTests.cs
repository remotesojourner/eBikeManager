using System.Net;
using EBikeManager.Application.Services;
using EBikeManager.IntegrationTests.Fixtures;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace EBikeManager.IntegrationTests.Web.Controllers;

public sealed class GoogleHealthControllerTests : IClassFixture<SetUpApp>, IClassFixture<SignInRequiredApp>
{
    private readonly SetUpApp _app;
    private readonly SignInRequiredApp _protectedApp;

    public GoogleHealthControllerTests(SetUpApp app, SignInRequiredApp protectedApp)
    {
        _app = app;
        _protectedApp = protectedApp;
    }

    [Fact]
    public async Task ARefusedSignInReturnsToTheIntegrationsSettingsWithTheReason()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var client = _app.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        using var response = await client.GetAsync(new Uri("/integrations/google-health/callback?error=access_denied&state=unknown", UriKind.Relative), cancellationToken);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/settings/google-health?googleHealth=failed", response.Headers.Location?.ToString());
        using var scope = _app.Services.CreateScope();
        Assert.Equal("Google reported a problem: access_denied", scope.ServiceProvider.GetRequiredService<GoogleHealthAccountService>().TakeSignInProblem());
    }

    [Fact]
    public async Task TheSignInCannotBeCompletedWithoutSigningInToEBikeManager()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var client = _protectedApp.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        using var response = await client.GetAsync(new Uri("/integrations/google-health/callback?code=google-code&state=unknown", UriKind.Relative), cancellationToken);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/auth/login?returnUrl=%2Fintegrations%2Fgoogle-health%2Fcallback%3Fcode%3Dgoogle-code%26state%3Dunknown", response.Headers.Location?.OriginalString);
    }
}
