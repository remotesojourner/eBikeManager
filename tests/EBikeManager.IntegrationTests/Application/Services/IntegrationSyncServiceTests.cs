using EBikeManager.Application.Configuration;
using EBikeManager.Application.Enums;
using EBikeManager.Application.Models;
using EBikeManager.Application.Models.Dtos;
using EBikeManager.Application.Repositories.Interfaces;
using EBikeManager.Application.Services;
using EBikeManager.IntegrationTests.Fixtures;
using EBikeManager.TestSupport;
using Microsoft.Extensions.DependencyInjection;

namespace EBikeManager.IntegrationTests.Application.Services;

public sealed class IntegrationSyncServiceTests
{
    [Fact]
    public async Task RidesSinceTheChosenDateAreUploadedOnceWithBoschsCalories()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var app = await StartAsync(GoogleHealthSettings.UploadFromValue(DateTime.UtcNow.AddDays(-10)), cancellationToken);

        var first = await RunAsync(app, cancellationToken);
        var second = await RunAsync(app, cancellationToken);

        Assert.Equal((5, 0), (first.Uploaded, second.Uploaded));
        Assert.Empty(first.Problems);
        Assert.Equal(
            ["ride-00", "ride-01", "ride-02", "ride-03", "ride-04"],
            app.Google.Uploads.Select(upload => upload.DataPointId.Replace("ebike-", "", StringComparison.Ordinal)).Order());
        Assert.All(app.Google.Uploads, upload => Assert.Equal(410, upload.CaloriesKcal));
        var export = Assert.Single(await ExportsAsync(app, "ride-00", cancellationToken));
        Assert.Equal(RideExportStatus.Uploaded, export.Status);
        Assert.Null(export.Problem);
        Assert.NotNull(export.ExportedAt);
    }

    [Fact]
    public async Task AllRidesAreUploadedWhenThatIsTheChoice()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var app = await StartAsync(GoogleHealthSettings.AllRides, cancellationToken);

        var result = await RunAsync(app, cancellationToken);

        Assert.Equal(30, result.Uploaded);
    }

    [Fact]
    public async Task ARideYourWatchAlsoRecordedIsMarkedFailedWithTheReasonAndNotUploaded()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var app = await StartAsync(GoogleHealthSettings.AllRides, cancellationToken);
        await AddWatchRideDuringAsync(app, "ride-00", cancellationToken);

        var first = await RunAsync(app, cancellationToken);
        var second = await RunAsync(app, cancellationToken);

        var export = (await ExportsAsync(app, "ride-00", cancellationToken))[0];
        Assert.Equal(RideExportStatus.WatchRecorded, export.Status);
        Assert.StartsWith("Your watch also recorded this ride (Outdoor bike", export.Problem, StringComparison.Ordinal);
        Assert.Contains("turn off automatic recognition of bike rides", export.Problem, StringComparison.Ordinal);
        Assert.DoesNotContain(app.Google.Uploads, upload => upload.DataPointId == FakeGoogleHealthApi.DataPointFor("ride-00"));
        Assert.Equal([FakeGoogleHealthApi.DataPointFor("ride-00")], app.Google.Removed);
        Assert.Equal((29, 0), (first.Uploaded, second.Uploaded));
        Assert.Equal("Google Health: 1 rides weren't uploaded because your watch also recorded them. See Settings, Google Health.", Assert.Single(first.Problems));
        Assert.Empty(second.Problems);
    }

    [Fact]
    public async Task UploadingARideYourWatchRecordedAgainStillSaysWhy()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var app = await StartAsync(GoogleHealthSettings.AllRides, cancellationToken);
        await AddWatchRideDuringAsync(app, "ride-00", cancellationToken);

        var result = await ExportOneAsync(app, "ride-00", cancellationToken);

        Assert.Equal(OperationOutcome.Invalid, result.Outcome);
        Assert.StartsWith("Your watch also recorded this ride", result.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task FailedUploadsAreReportedAndTriedAgainOnTheNextSync()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var app = await StartAsync(GoogleHealthSettings.UploadFromValue(DateTime.UtcNow.AddDays(-10)), cancellationToken);
        app.Google.FailingDataPoints.Add(FakeGoogleHealthApi.DataPointFor("ride-02"));

        var first = await RunAsync(app, cancellationToken);
        var failed = (await ExportsAsync(app, "ride-02", cancellationToken))[0];
        app.Google.FailingDataPoints.Clear();
        var second = await RunAsync(app, cancellationToken);

        Assert.Equal((4, 1), (first.Uploaded, second.Uploaded));
        Assert.Equal("Google Health: 1 rides couldn't be uploaded. They're tried again on the next sync.", Assert.Single(first.Problems));
        Assert.Equal(RideExportStatus.Failed, failed.Status);
        Assert.Contains("503", failed.Problem, StringComparison.Ordinal);
        Assert.Equal(RideExportStatus.Uploaded, (await ExportsAsync(app, "ride-02", cancellationToken))[0].Status);
    }

    [Fact]
    public async Task AnExpiredGoogleSignInIsReportedWithoutMarkingRidesFailed()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var app = await StartAsync(GoogleHealthSettings.AllRides, cancellationToken);
        app.Google.SignInExpired = true;

        var result = await RunAsync(app, cancellationToken);

        Assert.Equal(0, result.Uploaded);
        Assert.StartsWith("Google Health: ", Assert.Single(result.Problems), StringComparison.Ordinal);
        Assert.Empty(await ExportsAsync(app, "ride-00", cancellationToken));
    }

    [Fact]
    public async Task NothingIsUploadedUntilGoogleHealthIsConnected()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var app = new SetUpApp();
        await app.InitializeAsync();

        var result = await RunAsync(app, cancellationToken);

        Assert.Equal(0, result.Uploaded);
        Assert.Empty(result.FailedUploads ?? []);
        Assert.Empty(result.SignInsRequired ?? []);
        Assert.Empty(app.Google.Uploads);
    }

    [Fact]
    public async Task ASpecificRideCanBeUploadedEvenIfItIsOlderThanTheChosenDate()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var app = await StartAsync(GoogleHealthSettings.UploadFromValue(DateTime.UtcNow.AddDays(-10)), cancellationToken);

        var single = await ExportOneAsync(app, "ride-20", cancellationToken);
        var run = await RunAsync(app, cancellationToken);

        Assert.True(single.Succeeded, single.Message);
        Assert.Equal(5, run.Uploaded);
        Assert.Equal(6, app.Google.Uploads.Count);
        Assert.Single(app.Google.Uploads, upload => upload.DataPointId == FakeGoogleHealthApi.DataPointFor("ride-20"));
        Assert.Equal(RideExportStatus.Uploaded, (await ExportsAsync(app, "ride-20", cancellationToken))[0].Status);
    }

    [Fact]
    public async Task ASingleUploadIsRefusedWhenItCannotHappen()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var app = new SetUpApp();
        await app.InitializeAsync();

        var notConnected = await ExportOneAsync(app, "ride-00", cancellationToken);
        var unknownRide = await ExportOneAsync(app, "no-such-ride", cancellationToken);
        app.Services.GetRequiredService<SyncStateService>().TryStart("Syncing");
        var whileSyncing = await ExportOneAsync(app, "ride-00", cancellationToken);

        Assert.Equal((OperationOutcome.Invalid, "Google Health isn't connected."), (notConnected.Outcome, notConnected.Message));
        Assert.Equal(OperationOutcome.NotFound, unknownRide.Outcome);
        Assert.Equal(OperationOutcome.Conflict, whileSyncing.Outcome);
        Assert.Empty(app.Google.Uploads);
    }

    private static async Task<OperationResult> ExportOneAsync(SetUpApp app, string rideId, CancellationToken cancellationToken)
    {
        using var scope = app.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<IntegrationSyncService>().ExportOneAsync(GoogleHealthIntegration.IntegrationKey, rideId, cancellationToken);
    }

    private static async Task<SetUpApp> StartAsync(string uploadFrom, CancellationToken cancellationToken)
    {
        var app = new SetUpApp();
        await app.InitializeAsync();
        using var scope = app.Services.CreateScope();
        await SampleData.ConnectGoogleHealthAsync(scope.ServiceProvider, uploadFrom, cancellationToken);
        return app;
    }

    private static async Task<IntegrationRunResult> RunAsync(SetUpApp app, CancellationToken cancellationToken)
    {
        using var scope = app.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<IntegrationSyncService>().RunAsync(cancellationToken);
    }

    private static async Task<IReadOnlyList<RideExportDto>> ExportsAsync(SetUpApp app, string rideId, CancellationToken cancellationToken)
    {
        using var scope = app.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<IRideExportRepository>().GetForRideAsync(rideId, cancellationToken);
    }

    private static async Task<HealthExercise> AddWatchRideDuringAsync(SetUpApp app, string rideId, CancellationToken cancellationToken)
    {
        using var scope = app.Services.CreateScope();
        var ride = (await scope.ServiceProvider.GetRequiredService<IRideRepository>().FindAsync(rideId, cancellationToken))!;
        return app.Google.AddWatchRide(ride.StartTime.AddMinutes(5), ride.EndTime!.Value.AddMinutes(-5));
    }
}
