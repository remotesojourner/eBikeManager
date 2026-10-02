using System.Net;
using EBikeManager.Application.Models;
using EBikeManager.TestSupport;

namespace EBikeManager.UnitTests.Application.Services.Notifications;

public sealed class NotificationSendTestTests
{
    [Theory]
    [InlineData("ntfy", """{"url":"https://localhost/ntfy","topic":"rides","send_ride_synced":false}""", "New ride: Westminster to Blackfriars and back")]
    [InlineData("webhook", """{"url":"https://localhost/hook","send_ride_synced":false}""", "\"event\":\"TEST\"")]
    public async Task ATestIsSentEvenWhenNewRidesAreTurnedOff(string type, string settings, string expectedInBody)
    {
        using var handler = NotificationSamples.Answering();

        var result = await SendTestAsync(type, settings, handler);

        Assert.Equal(NotificationResult.Sent, result);
        Assert.Contains(expectedInBody, Assert.Single(handler.Requests).Body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ATestReportsTheStatusAndWhatTheServiceSaid()
    {
        using var handler = NotificationSamples.Answering(HttpStatusCode.Unauthorized, """{"code":40101,"error":"unauthorized"}""");

        var result = await SendTestAsync("ntfy", """{"url":"https://localhost/ntfy","topic":"rides"}""", handler);

        Assert.Equal(NotificationResult.Failed("""ntfy answered HTTP 401: {"code":40101,"error":"unauthorized"}"""), result);
    }

    [Fact]
    public async Task ATestReportsWhenTheServiceCannotBeReached()
    {
        using var handler = new RecordingHandler(_ => throw new HttpRequestException("No such host is known."));

        var result = await SendTestAsync("gotify", """{"url":"https://localhost/gotify","key":"AAAAAAAAAAAAAAA","priority":"5"}""", handler);

        Assert.Equal(NotificationResult.Failed("Couldn't reach Gotify: No such host is known."), result);
    }

    [Theory]
    [InlineData("ntfy", """{"url":"https://localhost/ntfy"}""", "The topic is missing")]
    [InlineData("telegram", """{"token":"1:abc"}""", "The bot token or chat ID is missing")]
    [InlineData("webhook", "{}", "The webhook URL is missing")]
    [InlineData("ntfy", "not json", "The settings can't be read")]
    [InlineData("carrierPigeon", "{}", "carrierPigeon isn't a known notification type")]
    public async Task ATestExplainsSettingsItCannotUse(string type, string settings, string expectedError)
    {
        using var handler = NotificationSamples.Answering();

        var result = await SendTestAsync(type, settings, handler);

        Assert.Equal(NotificationResult.Failed(expectedError), result);
        Assert.Empty(handler.Requests);
    }

    private static Task<NotificationResult> SendTestAsync(string type, string settings, RecordingHandler handler) =>
        TestNotifications.Dispatcher(new InMemoryNotificationChannels([]), handler)
            .TestAsync(type, "abc123", settings, NotificationSamples.Ride, TestContext.Current.CancellationToken);
}
