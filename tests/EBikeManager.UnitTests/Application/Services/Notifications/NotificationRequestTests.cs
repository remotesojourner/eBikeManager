using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using EBikeManager.Application.Enums;
using EBikeManager.Application.Models;
using EBikeManager.Application.Models.Events;
using EBikeManager.Application.Services;
using EBikeManager.TestSupport;
using static EBikeManager.TestSupport.Approvals;

namespace EBikeManager.UnitTests.Application.Services.Notifications;

public partial class NotificationRequestTests
{
    private const string AllVariables = "%title%|%bike%|%date%|%distance%|%moving_time%|%average_speed%|%elevation%|%calories%|%rider_share%|%service%|%error%|%year%-%month%-%day% %hour%:%minute%";

    private static readonly JsonSerializerOptions _indentedJson = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    private static readonly (string Scenario, string Type, string Settings, UnitSystem Units)[] _scenarios =
    [
        ("discord", "discord", """{"url":"https://localhost/discord.com/api/webhooks/1/x","display_name":"Rides"}""", UnitSystem.Metric),
        ("discord with every template variable", "discord", $$"""{"url":"https://localhost/discord.com/api/webhooks/1/x","ride_synced_message":"{{AllVariables}}","upload_failed_message":"{{AllVariables}}","sign_in_required_message":"{{AllVariables}}","sync_failed_message":"{{AllVariables}}","sync_restored_message":"{{AllVariables}}"}""", UnitSystem.Metric),
        ("discord without a url", "discord", """{"display_name":"Rides"}""", UnitSystem.Metric),
        ("discord saved from the settings form", "discord", """{"url":"https://localhost/discord.com/api/webhooks/1/x","display_name":"","send_ride_synced":true,"ride_synced_message":"","send_upload_failed":true,"upload_failed_message":"","send_sign_in_required":true,"sign_in_required_message":"","send_sync_failed":true,"sync_failed_message":"","send_sync_restored":true,"sync_restored_message":""}""", UnitSystem.Metric),
        ("telegram", "telegram", """{"token":"1:abc","chat_id":"-42"}""", UnitSystem.Metric),
        ("gotify", "gotify", """{"url":"https://localhost/gotify/","key":"AAAAAAAAAAAAAAA","priority":"4"}""", UnitSystem.Metric),
        ("gotify with every message turned off", "gotify", """{"url":"https://localhost/gotify","key":"AAAAAAAAAAAAAAA","send_ride_synced":false,"send_upload_failed":false,"send_sign_in_required":false,"send_sync_failed":false,"send_sync_restored":false}""", UnitSystem.Metric),
        ("ntfy", "ntfy", """{"url":"https://localhost/ntfy","topic":"rides","token":"tk_1","title":"eBike","tags":"bike","priority":"2","error_priority":"4"}""", UnitSystem.Metric),
        ("ntfy in imperial units", "ntfy", """{"url":"https://localhost/ntfy","topic":"rides"}""", UnitSystem.Imperial),
        ("ntfy saved from the settings form", "ntfy", """{"url":"https://localhost/ntfy","topic":"rides","token":"","title":"","tags":"","priority":"","error_priority":"","send_ride_synced":true,"ride_synced_message":"","send_upload_failed":true,"upload_failed_message":"","send_sign_in_required":true,"sign_in_required_message":"","send_sync_failed":true,"sync_failed_message":"","send_sync_restored":true,"sync_restored_message":""}""", UnitSystem.Metric),
        ("pushover", "pushover", """{"token":"aaaaaaaaaaaaaaaaaaaaaaaaaaaaaa","user_key":"bbbbbbbbbbbbbbbbbbbbbbbbbbbbbb"}""", UnitSystem.Metric),
        ("apprise with urls", "apprise", """{"url":"https://localhost/apprise/","urls":"json://localhost/hook, mailto://me@example.com","title":"eBike"}""", UnitSystem.Metric),
        ("apprise with a config key and tags", "apprise", """{"url":"https://localhost/apprise","key":"home","tags":"admin, bikes"}""", UnitSystem.Metric),
        ("apprise with urls and a config key", "apprise", """{"url":"https://localhost/apprise","urls":"json://localhost/hook","key":"home"}""", UnitSystem.Metric),
        ("webhook", "webhook", """{"url":"https://localhost/hook","send_alive":true}""", UnitSystem.Metric),
        ("webhook with every event turned off", "webhook", """{"url":"https://localhost/hook","send_ride_synced":false,"send_upload_failed":false,"send_sign_in_required":false,"send_sync_failed":false,"send_sync_restored":false}""", UnitSystem.Imperial),
        ("unknown type", "carrierPigeon", """{"url":"https://localhost/coop"}""", UnitSystem.Metric)
    ];

    [Fact]
    public async Task EveryNotificationSendsTheApprovedRequests()
    {
        var transcript = new StringBuilder();
        var previousCulture = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
        try
        {
            foreach (var (scenario, type, settings, units) in _scenarios)
            {
                foreach (var (eventName, notificationEvent) in Events())
                {
                    using var handler = NotificationSamples.Answering();
                    var channels = NotificationSamples.Channels(type, settings);

                    await TestNotifications.Dispatcher(channels, handler, units).PublishAsync(notificationEvent, TestContext.Current.CancellationToken);

                    Record(transcript, $"{scenario} · {eventName}", $"activity: {Activity(channels.ActivityErrors)}", handler);
                }

                using var testHandler = NotificationSamples.Answering();
                var testChannels = NotificationSamples.Channels(type, "{}");
                var result = await TestNotifications.Dispatcher(testChannels, testHandler, units)
                    .TestAsync(type, "abc123", settings, NotificationSamples.Ride, TestContext.Current.CancellationToken);

                Record(transcript, $"{scenario} · send test", $"result: {TestResult(result)}, activity: {Activity(testChannels.ActivityErrors)}", testHandler);
            }
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
        }

        AssertMatchesApproved("NotificationRequests.approved.txt", transcript.ToString());
    }

    [Fact]
    public void TheSettingsFormsForEveryNotificationTypeStayTheSame()
    {
        using var handler = NotificationSamples.Answering();
        var dispatcher = TestNotifications.Dispatcher(new InMemoryNotificationChannels([]), handler);

        AssertMatchesApproved("NotificationSchemas.approved.json", JsonSerializer.Serialize(dispatcher.Schemas, _indentedJson) + "\n");
    }

    private static IEnumerable<(string Name, NotificationEvent Event)> Events() =>
    [
        ("ride synced", new RideSynced(NotificationSamples.Ride)),
        ("untitled ride synced", new RideSynced(NotificationSamples.UntitledRide)),
        ("upload failed", new UploadFailed(NotificationSamples.Ride, "Google Health", "Google Health answered 503")),
        ("sign-in required", new SignInRequired(SyncRunService.BoschService)),
        ("sync failed", new SyncFailed("Bosch answered 503 Service Unavailable")),
        ("sync restored", new SyncRestored()),
        ("heartbeat", new Heartbeat())
    ];

    private static void Record(StringBuilder transcript, string heading, string outcome, RecordingHandler handler)
    {
        transcript.Append("## ").AppendLine(heading);
        transcript.AppendLine(outcome);
        foreach (var request in handler.Requests)
        {
            transcript.Append(request.Method).Append(' ').AppendLine(request.Uri?.ToString());
            foreach (var header in request.Headers) transcript.AppendLine(header);
            transcript.AppendLine(SendTime().Replace(request.Body, "<send time>"));
        }

        transcript.AppendLine();
    }

    private static string TestResult(NotificationResult result) => result.Outcome switch
    {
        NotificationOutcome.Failed => $"failed ({result.Error})",
        var outcome => outcome.ToString().ToLowerInvariant()
    };

    private static string Activity(IReadOnlyList<bool> errors) => errors switch
    {
        [] => "unchanged",
        [false] => "sent",
        [true] => "failed",
        _ => string.Join(",", errors.Select(error => error ? "failed" : "sent"))
    };

    [GeneratedRegex(@"\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}\.\d+Z")]
    private static partial Regex SendTime();
}
