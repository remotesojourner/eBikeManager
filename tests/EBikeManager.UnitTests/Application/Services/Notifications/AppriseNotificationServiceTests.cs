using System.Net;
using System.Text.Json;
using EBikeManager.Application.Models;
using EBikeManager.Application.Models.Events;
using EBikeManager.TestSupport;

namespace EBikeManager.UnitTests.Application.Services.Notifications;

public sealed class AppriseNotificationServiceTests
{
    private const string WithUrls = """{"url":"http://apprise:8000/","urls":"json://listener/hook"}""";
    private const string WithKey = """{"url":"http://apprise:8000","key":"home"}""";

    public static TheoryData<string, NotificationEvent> MessageTypes => new()
    {
        { "success", new RideSynced(NotificationSamples.Ride) },
        { "success", new SyncRestored() },
        { "failure", new SyncFailed("Bosch answered 503") },
        { "failure", new UploadFailed(NotificationSamples.Ride, "Google Health", "Google Health answered 503") },
        { "warning", new SignInRequired("Google Health") }
    };

    [Theory]
    [MemberData(nameof(MessageTypes))]
    public async Task EachMessageIsSentWithTheMatchingAppriseType(string expectedType, NotificationEvent notificationEvent)
    {
        using var handler = NotificationSamples.Answering();

        await TestNotifications.Dispatcher(NotificationSamples.Channels("apprise", WithUrls), handler).PublishAsync(notificationEvent, TestContext.Current.CancellationToken);

        using var body = JsonDocument.Parse(Assert.Single(handler.Requests).Body);
        Assert.Equal(expectedType, body.RootElement.GetProperty("type").GetString());
    }

    [Theory]
    [InlineData(WithUrls, "Apprise found no valid URLs to send to")]
    [InlineData(WithKey, "Apprise has no configuration for the key home")]
    public async Task NothingToSendToIsAFailureThoughAppriseAnswers204(string settings, string expectedError)
    {
        using var handler = NotificationSamples.Answering(HttpStatusCode.NoContent);

        Assert.Equal(NotificationResult.Failed(expectedError), await SendTestAsync(settings, handler));
    }

    [Fact]
    public async Task AFailedDeliveryShowsWhatAppriseLogged()
    {
        using var handler = NotificationSamples.Answering(HttpStatusCode.FailedDependency,
            """{"error": "One or more notifications could not be sent", "details": [["INFO", "2026-09-17 03:28:04,050", "Notifying 2 service(s) with threads."], ["WARNING", "2026-09-17 03:28:04,135", "Failed to send JSON POST notification: Verification Failed., error=401."]]}""");

        Assert.Equal(
            NotificationResult.Failed("Apprise answered HTTP 424: One or more notifications could not be sent: Failed to send JSON POST notification: Verification Failed., error=401."),
            await SendTestAsync(WithUrls, handler));
    }

    [Theory]
    [InlineData("""{"url":"http://apprise:8000","key":"home","tags":"admin bikes"}""", "Apprise has nothing tagged admin bikes to notify")]
    [InlineData(WithKey, "Apprise has nothing untagged to notify. Add tags, or all, to choose what to notify")]
    public async Task TagsThatMatchNothingAreExplained(string settings, string expectedError)
    {
        using var handler = NotificationSamples.Answering(HttpStatusCode.FailedDependency, """{"error": "One or more notification could not be sent", "details": []}""");

        Assert.Equal(NotificationResult.Failed(expectedError), await SendTestAsync(settings, handler));
    }

    [Fact]
    public async Task UrlsAndAConfigKeyTogetherAreRefusedWithoutSending()
    {
        using var handler = NotificationSamples.Answering();

        var result = await SendTestAsync("""{"url":"http://apprise:8000","urls":"json://listener/hook","key":"home"}""", handler);

        Assert.Equal(NotificationResult.Failed("Use either Apprise URLs or a config key, not both"), result);
        Assert.Empty(handler.Requests);
    }

    private static Task<NotificationResult> SendTestAsync(string settings, RecordingHandler handler) =>
        TestNotifications.Dispatcher(new InMemoryNotificationChannels([]), handler)
            .TestAsync("apprise", "abc123", settings, NotificationSamples.Ride, TestContext.Current.CancellationToken);
}
