using EBikeManager.Application.Enums;
using EBikeManager.Application.Models.Entities;
using EBikeManager.Application.Models.Events;
using EBikeManager.Application.Services;
using EBikeManager.TestSupport;

namespace EBikeManager.UnitTests.Application.Services.Notifications;

public sealed class NotificationDispatchServiceTests
{
    private const string Ntfy = """{"url":"https://localhost/ntfy","topic":"rides"}""";

    [Theory]
    [InlineData("discord", """{"url":"https://localhost/discord.com/api/webhooks/1/x"}""")]
    [InlineData("telegram", """{"token":"1:abc","chat_id":"42"}""")]
    [InlineData("gotify", """{"url":"https://localhost/gotify","key":"AAAAAAAAAAAAAAA","priority":"5"}""")]
    [InlineData("ntfy", Ntfy)]
    [InlineData("pushover", """{"token":"aaaaaaaaaaaaaaaaaaaaaaaaaaaaaa","user_key":"bbbbbbbbbbbbbbbbbbbbbbbbbbbbbb"}""")]
    [InlineData("apprise", """{"url":"https://localhost/apprise","urls":"json://localhost/hook"}""")]
    [InlineData("webhook", """{"url":"https://localhost/hook"}""")]
    public async Task ANewRideIsSentToEveryNotificationByDefault(string type, string settings)
    {
        using var handler = NotificationSamples.Answering();

        await TestNotifications.Dispatcher(NotificationSamples.Channels(type, settings), handler)
            .PublishAsync(new RideSynced(NotificationSamples.Ride), TestContext.Current.CancellationToken);

        Assert.Contains("Westminster to Blackfriars and back", Assert.Single(handler.Requests).Body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AMessageTurnedOffIsNotSentAndLeavesLastRunAlone()
    {
        using var handler = NotificationSamples.Answering();
        var channels = NotificationSamples.Channels("ntfy", """{"url":"https://localhost/ntfy","topic":"rides","send_ride_synced":false}""");

        await TestNotifications.Dispatcher(channels, handler).PublishAsync(new RideSynced(NotificationSamples.Ride), TestContext.Current.CancellationToken);

        Assert.Empty(handler.Requests);
        Assert.Empty(channels.ActivityErrors);
    }

    [Fact]
    public async Task ACustomMessageReplacesTheDefaultOneAndASettingsFormBlankDoesNot()
    {
        using var custom = NotificationSamples.Answering();
        using var blank = NotificationSamples.Answering();

        await TestNotifications.Dispatcher(NotificationSamples.Channels("ntfy", """{"url":"https://localhost/ntfy","topic":"rides","ride_synced_message":"%title%: %distance%"}"""), custom)
            .PublishAsync(new RideSynced(NotificationSamples.Ride), TestContext.Current.CancellationToken);
        await TestNotifications.Dispatcher(NotificationSamples.Channels("ntfy", """{"url":"https://localhost/ntfy","topic":"rides","ride_synced_message":""}"""), blank)
            .PublishAsync(new RideSynced(NotificationSamples.Ride), TestContext.Current.CancellationToken);

        Assert.Equal("Westminster to Blackfriars and back: 4.1 km", Assert.Single(custom.Requests).Body);
        Assert.StartsWith("New ride: Westminster to Blackfriars and back", Assert.Single(blank.Requests).Body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task MessagesUseTheChosenUnits()
    {
        using var handler = NotificationSamples.Answering();
        var settings = """{"url":"https://localhost/ntfy","topic":"rides","ride_synced_message":"%distance%|%average_speed%|%elevation%|%moving_time%|%calories%|%rider_share%|%date%"}""";

        await TestNotifications.Dispatcher(NotificationSamples.Channels("ntfy", settings), handler, UnitSystem.Imperial)
            .PublishAsync(new RideSynced(NotificationSamples.Ride), TestContext.Current.CancellationToken);

        Assert.Equal("2.5 mi|14.9 mph|59 ft|10 min|104 kcal|64%|2026-09-27 08:14", Assert.Single(handler.Requests).Body);
    }

    [Fact]
    public async Task UnreadableSettingsAreNotSentAndShowAsFailed()
    {
        using var handler = NotificationSamples.Answering();
        var logger = new RecordingLogger<NotificationDispatchService>();
        var channels = NotificationSamples.Channels("ntfy", "not json");

        await TestNotifications.Dispatcher(channels, handler, logger: logger).PublishAsync(new SyncRestored(), TestContext.Current.CancellationToken);

        Assert.Empty(handler.Requests);
        Assert.Equal([true], channels.ActivityErrors);
        Assert.Contains(logger.Warnings, warning => warning.Contains("can't be read", StringComparison.Ordinal));
    }

    [Fact]
    public async Task AFailedDeliveryIsLoggedWithTheReasonAndShowsAsFailed()
    {
        using var handler = NotificationSamples.Answering(System.Net.HttpStatusCode.Forbidden, "topic is reserved");
        var logger = new RecordingLogger<NotificationDispatchService>();
        var channels = NotificationSamples.Channels("ntfy", Ntfy);

        await TestNotifications.Dispatcher(channels, handler, logger: logger).PublishAsync(new SyncFailed("Bosch answered 503"), TestContext.Current.CancellationToken);

        Assert.Equal([true], channels.ActivityErrors);
        Assert.Contains(logger.Warnings, warning => warning.Contains("ntfy answered HTTP 403: topic is reserved", StringComparison.Ordinal));
    }

    [Fact]
    public async Task OneBrokenChannelDoesNotStopTheOthers()
    {
        using var handler = NotificationSamples.Answering();
        var channels = new InMemoryNotificationChannels(
        [
            new NotificationChannel { Id = "broken", Type = "carrierPigeon", Data = "{}" },
            new NotificationChannel { Id = "working", Type = "ntfy", Data = Ntfy }
        ]);

        await TestNotifications.Dispatcher(channels, handler).PublishAsync(new SyncRestored(), TestContext.Current.CancellationToken);

        Assert.Equal([true, false], channels.ActivityErrors);
        Assert.Single(handler.Requests);
    }

    [Theory]
    [InlineData(1, 6)]
    [InlineData(5, 2)]
    public async Task TheKeepAliveIsSentAtTheChosenInterval(int intervalMinutes, int expectedPosts)
    {
        using var handler = NotificationSamples.Answering();
        var time = new FixedTime(TestNotifications.Now);
        var dispatcher = TestNotifications.Dispatcher(
            NotificationSamples.Channels("webhook", $$"""{"url":"https://localhost/hook","send_alive":true,"interval":{{intervalMinutes}}}"""), handler, time: time);

        for (var minute = 0; minute < 6; minute++)
        {
            await dispatcher.PublishAsync(new Heartbeat(), TestContext.Current.CancellationToken);
            time.Now = time.Now.AddMinutes(1);
        }

        Assert.Equal(expectedPosts, handler.Requests.Count);
        Assert.All(handler.Requests, request => Assert.Contains("KEEP_ALIVE", request.Body, StringComparison.Ordinal));
    }
}
