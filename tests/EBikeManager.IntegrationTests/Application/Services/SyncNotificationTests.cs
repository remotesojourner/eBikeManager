using System.Text.Json;
using EBikeManager.Application.Configuration;
using EBikeManager.Application.Exceptions;
using EBikeManager.Application.Repositories.Interfaces;
using EBikeManager.Application.Services;
using EBikeManager.IntegrationTests.Fixtures;
using Microsoft.Extensions.DependencyInjection;

namespace EBikeManager.IntegrationTests.Application.Services;

public sealed class SyncNotificationTests
{
    [Fact]
    public async Task OnlyRidesNewSinceTheLastSyncAreAnnounced()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var app = await StartAsync(cancellationToken);
        app.Bosch.AddRide("ride-new", SampleData.BikeId, DateTime.UtcNow.AddHours(-2), title: "Evening ride");

        await SyncAsync(app, cancellationToken);
        await SyncAsync(app, cancellationToken);

        var announced = Assert.Single(Events(app));
        Assert.Equal("RIDE_SYNCED", announced.Event);
        Assert.Equal("ride-new", announced.Data.GetProperty("id").GetString());
        Assert.Equal("Evening ride", announced.Data.GetProperty("title").GetString());
    }

    [Fact]
    public async Task AFailingSyncIsAnnouncedOnceAndSoIsItsRecovery()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var app = await StartAsync(cancellationToken);
        app.Bosch.ActivitiesFailure = new HttpRequestException("Bosch answered 503 Service Unavailable");

        await SyncAsync(app, cancellationToken);
        await SyncAsync(app, cancellationToken);
        app.Bosch.ActivitiesFailure = null;
        await SyncAsync(app, cancellationToken);
        await SyncAsync(app, cancellationToken);

        Assert.Equal(["SYNC_FAILED", "SYNC_RESTORED"], Events(app).Select(sent => sent.Event));
        Assert.Equal("Bosch answered 503 Service Unavailable", Events(app)[0].Data.GetProperty("error").GetString());
    }

    [Fact]
    public async Task ABoschSignInIsAskedForOnceAndNotReportedAsAFailedSync()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var app = await StartAsync(cancellationToken);
        app.Bosch.ActivitiesFailure = new BoschReauthRequiredException("invalid_grant");

        await SyncAsync(app, cancellationToken);
        await SyncAsync(app, cancellationToken);

        var asked = Assert.Single(Events(app));
        Assert.Equal("SIGN_IN_REQUIRED", asked.Event);
        Assert.Equal(SyncRunService.BoschService, asked.Data.GetProperty("service").GetString());
    }

    [Fact]
    public async Task ARideThatFailsToUploadIsAnnouncedOnItsFirstFailureOnly()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var app = await StartAsync(cancellationToken);
        await ConnectGoogleHealthAsync(app, cancellationToken);
        app.Google.FailingDataPoints.Add("ebike-ride-03");

        await SyncAsync(app, cancellationToken);
        await SyncAsync(app, cancellationToken);

        var failed = Assert.Single(Events(app));
        Assert.Equal("UPLOAD_FAILED", failed.Event);
        Assert.Equal("Google Health", failed.Data.GetProperty("service").GetString());
        Assert.Equal("ride-03", failed.Data.GetProperty("ride").GetProperty("id").GetString());
        Assert.Contains("503", failed.Data.GetProperty("error").GetString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task AnExpiredGoogleHealthSignInIsAskedForOnceUntilYouSignInAgain()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var app = await StartAsync(cancellationToken);
        await ConnectGoogleHealthAsync(app, cancellationToken);
        app.Google.SignInExpired = true;

        await SyncAsync(app, cancellationToken);
        await SyncAsync(app, cancellationToken);
        app.Services.GetRequiredService<NotificationStateService>().SignedIn("Google Health");
        await SyncAsync(app, cancellationToken);

        Assert.Equal(["SIGN_IN_REQUIRED", "SIGN_IN_REQUIRED"], Events(app).Select(sent => sent.Event));
        Assert.All(Events(app), sent => Assert.Equal("Google Health", sent.Data.GetProperty("service").GetString()));
    }

    private static async Task<SetUpApp> StartAsync(CancellationToken cancellationToken)
    {
        var app = new SetUpApp();
        await app.InitializeAsync();
        using var scope = app.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<INotificationChannelRepository>().CreateAsync("webhook", "Hook", """{"url":"https://localhost/hook"}""", cancellationToken);
        return app;
    }

    private static async Task ConnectGoogleHealthAsync(SetUpApp app, CancellationToken cancellationToken)
    {
        using var scope = app.Services.CreateScope();
        await SampleData.ConnectGoogleHealthAsync(scope.ServiceProvider, GoogleHealthSettings.AllRides, cancellationToken);
    }

    private static async Task SyncAsync(SetUpApp app, CancellationToken cancellationToken)
    {
        using var scope = app.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<SyncRunService>().RunScheduledAsync(cancellationToken);
    }

    private static List<(string Event, JsonElement Data)> Events(SetUpApp app) =>
    [
        .. app.Notifications.Requests.Select(request =>
        {
            using var body = JsonDocument.Parse(request.Body);
            return (body.RootElement.GetProperty("event").GetString()!, body.RootElement.GetProperty("data").Clone());
        })
    ];
}
