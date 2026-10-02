using System.Text.Json;
using EBikeManager.Application.Enums;
using EBikeManager.Application.Services;
using EBikeManager.IntegrationTests.Fixtures;
using EBikeManager.TestSupport;
using Microsoft.Extensions.DependencyInjection;

namespace EBikeManager.IntegrationTests.Application.Services;

public sealed class NotificationServiceTests : IClassFixture<SetUpApp>
{
    private readonly SetUpApp _app;

    public NotificationServiceTests(SetUpApp app)
    {
        _app = app;
    }

    [Fact]
    public async Task ANotificationIsSavedWithItsNameApartFromItsSettingsAndCanBeChangedAndDeleted()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var scope = _app.Services.CreateScope();
        var notifications = Service(scope, FixedAccess.Full);

        var created = await notifications.CreateAsync("ntfy", "Phone", Settings("""{"url":"https://localhost/ntfy","topic":"rides"}"""), cancellationToken);
        var id = created.Value!;
        var updated = await notifications.UpdateAsync(id, "My phone", Settings("""{"send_sync_restored":false}"""), cancellationToken);

        var saved = Assert.Single(await notifications.GetChannelsAsync(cancellationToken), channel => channel.Id == id);
        Assert.True(updated.Succeeded);
        Assert.Equal(("ntfy", "My phone"), (saved.Type, saved.DisplayName));
        Assert.Equal("rides", saved.Data["topic"]?.ToString());
        Assert.Equal("False", saved.Data["send_sync_restored"]?.ToString());

        Assert.True((await notifications.DeleteAsync(id, cancellationToken)).Succeeded);
        Assert.DoesNotContain(await notifications.GetChannelsAsync(cancellationToken), channel => channel.Id == id);
    }

    [Theory]
    [InlineData("""{"url":"https://localhost/ntfy"}""", "topic is required")]
    [InlineData("""{"url":"https://localhost/ntfy","topic":"has spaces"}""", "topic doesn't have the expected format")]
    [InlineData("""{"url":"https://localhost/ntfy","topic":"rides","colour":"red"}""", "colour isn't a setting of the ntfy notification")]
    public async Task SettingsTheTypeCannotUseAreRefused(string settings, string expectedProblem)
    {
        using var scope = _app.Services.CreateScope();

        var result = await Service(scope, FixedAccess.Full).CreateAsync("ntfy", null, Settings(settings), TestContext.Current.CancellationToken);

        Assert.Equal((OperationOutcome.Invalid, expectedProblem), (result.Outcome, result.Message));
    }

    [Fact]
    public async Task ATestUsesTheNewestFinishedRideWithoutSavingAnything()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var scope = _app.Services.CreateScope();
        var notifications = Service(scope, FixedAccess.Full);
        var before = _app.Notifications.Requests.Count;

        var result = await notifications.SendTestAsync("webhook", Settings("""{"url":"https://localhost/test-hook"}"""), null, cancellationToken);

        Assert.True(result.Value!.Success);
        var request = Assert.Single(_app.Notifications.Requests.Skip(before));
        Assert.Equal("https://localhost/test-hook", request.Uri?.ToString());
        Assert.Contains("\"event\":\"TEST\"", request.Body, StringComparison.Ordinal);
        Assert.Contains("\"id\":\"ride-00\"", request.Body, StringComparison.Ordinal);
        Assert.DoesNotContain(await notifications.GetChannelsAsync(cancellationToken), channel => channel.Data.TryGetValue("url", out var url) && url?.ToString() == "https://localhost/test-hook");
    }

    [Fact]
    public async Task VisitorsWithoutAccessCannotChangeOrTestNotifications()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var scope = _app.Services.CreateScope();
        var notifications = Service(scope, FixedAccess.None);
        var settings = Settings("""{"url":"https://localhost/ntfy","topic":"rides"}""");

        Assert.Equal(OperationOutcome.Denied, (await notifications.CreateAsync("ntfy", null, settings, cancellationToken)).Outcome);
        Assert.Equal(OperationOutcome.Denied, (await notifications.UpdateAsync("any", null, settings, cancellationToken)).Outcome);
        Assert.Equal(OperationOutcome.Denied, (await notifications.DeleteAsync("any", cancellationToken)).Outcome);
        Assert.Equal(OperationOutcome.Denied, (await notifications.SendTestAsync("ntfy", settings, null, cancellationToken)).Outcome);
    }

    [Fact]
    public async Task AnUnknownTypeOrChannelIsNotFound()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var scope = _app.Services.CreateScope();
        var notifications = Service(scope, FixedAccess.Full);

        Assert.Equal(OperationOutcome.NotFound, (await notifications.CreateAsync("carrierPigeon", null, Settings("{}"), cancellationToken)).Outcome);
        Assert.Equal(OperationOutcome.NotFound, (await notifications.SendTestAsync("carrierPigeon", Settings("{}"), null, cancellationToken)).Outcome);
        Assert.Equal(OperationOutcome.NotFound, (await notifications.UpdateAsync("missing", null, Settings("{}"), cancellationToken)).Outcome);
        Assert.Equal(OperationOutcome.NotFound, (await notifications.DeleteAsync("missing", cancellationToken)).Outcome);
    }

    private static NotificationService Service(IServiceScope scope, FixedAccess access) =>
        ActivatorUtilities.CreateInstance<NotificationService>(scope.ServiceProvider, access);

    private static Dictionary<string, JsonElement> Settings(string json) => JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json)!;
}
