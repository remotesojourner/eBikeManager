using EBikeManager.Application.Models;
using EBikeManager.Application.Models.Dtos;
using EBikeManager.Application.Models.Events;
using EBikeManager.Application.Resources;
using EBikeManager.Application.Utils;

namespace EBikeManager.Application.Services.Notifications;

internal sealed class WebhookNotificationService : HttpNotificationService
{
    public WebhookNotificationService(IHttpClientFactory httpClientFactory) : base(httpClientFactory)
    {
    }

    public override string Name => "webhook";

    public override NotificationTypeSchemaDto Schema { get; } = new()
    {
        Name = "webhook",
        Title = "Webhook",
        Description = "Post every event as JSON, for Home Assistant, Node-RED, n8n or your own scripts",
        Fields =
        [
            new() { Name = "url", Type = "text", Required = true, Regex = @"https?://.+", Placeholder = "https://example.com/webhook" },
            new() { Name = "send_ride_synced", Type = "boolean", Default = true },
            new() { Name = "send_upload_failed", Type = "boolean", Default = true },
            new() { Name = "send_sign_in_required", Type = "boolean", Default = true },
            new() { Name = "send_sync_failed", Type = "boolean", Default = true },
            new() { Name = "send_sync_restored", Type = "boolean", Default = true },
            new() { Name = "send_alive", Type = "boolean", Default = false },
            new() { Name = "interval", Type = "number", Default = 1 }
        ]
    };

    public override Task<NotificationResult> SendTestAsync(NotificationContext context, RideDto sample, CancellationToken cancellationToken)
    {
        var url = context.Settings.GetString("url");
        return string.IsNullOrEmpty(url)
            ? Task.FromResult(NotificationResult.Failed(ApplicationStrings.WebhookUrlMissing))
            : PostAsync(url, "TEST", NotificationEventData.Ride(sample), cancellationToken);
    }

    public override Task<NotificationResult> HandleAsync(NotificationEvent notificationEvent, NotificationContext context, CancellationToken cancellationToken)
    {
        var settings = context.Settings;
        var url = settings.GetString("url");
        if (string.IsNullOrEmpty(url)) return Task.FromResult(NotificationResult.Failed(ApplicationStrings.WebhookUrlMissing));

        var eventName = notificationEvent switch
        {
            Heartbeat when settings.GetBool("send_alive") => "KEEP_ALIVE",
            RideSynced when settings.GetBool("send_ride_synced", true) => "RIDE_SYNCED",
            UploadFailed when settings.GetBool("send_upload_failed", true) => "UPLOAD_FAILED",
            SignInRequired when settings.GetBool("send_sign_in_required", true) => "SIGN_IN_REQUIRED",
            SyncFailed when settings.GetBool("send_sync_failed", true) => "SYNC_FAILED",
            SyncRestored when settings.GetBool("send_sync_restored", true) => "SYNC_RESTORED",
            _ => null
        };

        return eventName == null
            ? Task.FromResult(NotificationResult.NotApplicable)
            : PostAsync(url, eventName, NotificationEventData.For(notificationEvent), cancellationToken);
    }

    private Task<NotificationResult> PostAsync(string url, string eventName, object? data, CancellationToken cancellationToken)
    {
        var request = JsonPost(url, new { @event = eventName, data });
        request.Headers.Add("User-Agent", $"{ProjectInfo.Name.Replace(" ", "", StringComparison.Ordinal)}/Webhook");
        return SendAsync(request, cancellationToken);
    }
}
