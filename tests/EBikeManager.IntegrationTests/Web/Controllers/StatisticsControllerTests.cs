using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using EBikeManager.Application.Enums;
using EBikeManager.Application.Services;
using EBikeManager.Application.Utils;
using EBikeManager.IntegrationTests.Fixtures;
using EBikeManager.TestSupport;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace EBikeManager.IntegrationTests.Web.Controllers;

public sealed class StatisticsControllerTests : IClassFixture<SetUpApp>, IClassFixture<SignInRequiredApp>
{
    private static readonly Uri _statistics = new("/api/statistics", UriKind.Relative);

    private readonly SetUpApp _app;
    private readonly SignInRequiredApp _protectedApp;

    public StatisticsControllerTests(SetUpApp app, SignInRequiredApp protectedApp)
    {
        _app = app;
        _protectedApp = protectedApp;
    }

    [Fact]
    public async Task TheStatisticsAddUpEveryRideAndCountTheBikes()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var client = _app.CreateClient();

        using var response = await client.GetAsync(_statistics, cancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        var statistics = json.RootElement;
        Assert.Equal(1, statistics.GetProperty("bikes").GetInt32());
        Assert.Equal(30, statistics.GetProperty("rides").GetInt32());
        Assert.Equal(600, statistics.GetProperty("mileageKm").GetDouble());
        Assert.Equal(99_000, statistics.GetProperty("movingTimeSeconds").GetInt64());
        Assert.Equal(3_600, statistics.GetProperty("elevationGainMeters").GetDouble());
        Assert.Equal(12_300, statistics.GetProperty("caloriesKcal").GetDouble());
    }

    [Fact]
    public async Task WithSignInOnTheStatisticsNeedTheApiTokenOrASignedInUser()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var token = await ApiTokenAsync(regenerate: false, cancellationToken);

        using var anonymous = await GetAsync(_statistics, null, cancellationToken);
        using var wrongToken = await GetAsync(_statistics, ApiToken.Generate(), cancellationToken);
        using var withToken = await GetAsync(_statistics, token, cancellationToken);
        using var signedIn = await _protectedApp.CreateSignedInClientAsync(cancellationToken);
        using var withSignIn = await signedIn.GetAsync(_statistics, cancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
        Assert.Equal("Bearer", anonymous.Headers.WwwAuthenticate.ToString());
        Assert.Equal(HttpStatusCode.Unauthorized, wrongToken.StatusCode);
        Assert.Equal(HttpStatusCode.OK, withToken.StatusCode);
        Assert.Equal(HttpStatusCode.OK, withSignIn.StatusCode);
    }

    [Fact]
    public async Task TheApiTokenOpensNothingButTheStatistics()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var token = await ApiTokenAsync(regenerate: false, cancellationToken);

        using var page = await GetAsync(new Uri("/rides", UriKind.Relative), token, cancellationToken);
        using var picture = await GetAsync(new Uri($"/bikes/{SampleData.BikeId}/picture?v=1", UriKind.Relative), token, cancellationToken);

        Assert.Equal(HttpStatusCode.Redirect, page.StatusCode);
        Assert.StartsWith("/auth/login", page.Headers.Location?.OriginalString, StringComparison.Ordinal);
        Assert.Equal(HttpStatusCode.Unauthorized, picture.StatusCode);
    }

    [Fact]
    public async Task ANewTokenReplacesTheOldOne()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var old = await ApiTokenAsync(regenerate: false, cancellationToken);
        var renewed = await ApiTokenAsync(regenerate: true, cancellationToken);

        using var withOld = await GetAsync(_statistics, old, cancellationToken);
        using var withRenewed = await GetAsync(_statistics, renewed, cancellationToken);

        Assert.NotEqual(old, renewed);
        Assert.Equal(HttpStatusCode.Unauthorized, withOld.StatusCode);
        Assert.Equal(HttpStatusCode.OK, withRenewed.StatusCode);
    }

    [Fact]
    public async Task ThereIsNoTokenWhileSignInIsOff()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var scope = _app.Services.CreateScope();

        var result = await ActivatorUtilities.CreateInstance<SignInService>(scope.ServiceProvider, FixedAccess.Full).GetApiTokenAsync(cancellationToken);

        Assert.Equal(OperationOutcome.Invalid, result.Outcome);
    }

    private async Task<string> ApiTokenAsync(bool regenerate, CancellationToken cancellationToken)
    {
        using var scope = _protectedApp.Services.CreateScope();
        var signIn = ActivatorUtilities.CreateInstance<SignInService>(scope.ServiceProvider, FixedAccess.Full);
        var result = regenerate ? await signIn.RegenerateApiTokenAsync(cancellationToken) : await signIn.GetApiTokenAsync(cancellationToken);
        Assert.True(result.Succeeded, result.Message);
        return result.Value!;
    }

    private async Task<HttpResponseMessage> GetAsync(Uri address, string? token, CancellationToken cancellationToken)
    {
        using var client = _protectedApp.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        using var request = new HttpRequestMessage(HttpMethod.Get, address);
        if (token != null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await client.SendAsync(request, cancellationToken);
    }
}
