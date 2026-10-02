using EBikeManager.Application.Utils;
using EBikeManager.Web.Resources;

namespace EBikeManager.Web.Utils;

public static class NotificationText
{
    public static string Label(string type, string field) =>
        OwnLabel(type, field) ?? CommonLabel(field) ?? Humanize(field);

    public static string? Placeholder(string type, string field, string? schemaPlaceholder) =>
        OwnPlaceholder(type, field) ?? CommonPlaceholder(field) ?? schemaPlaceholder;

    private static string? CommonLabel(string field) => field switch
    {
        "url" => WebStrings.NotificationFieldUrl,
        "token" => WebStrings.NotificationFieldToken,
        "display_name" => WebStrings.NotificationFieldDisplayName,
        "send_ride_synced" => WebStrings.NotificationFieldSendRideSynced,
        "ride_synced_message" => WebStrings.NotificationFieldRideSyncedMessage,
        "send_upload_failed" => WebStrings.NotificationFieldSendUploadFailed,
        "upload_failed_message" => WebStrings.NotificationFieldUploadFailedMessage,
        "send_sign_in_required" => WebStrings.NotificationFieldSendSignInRequired,
        "sign_in_required_message" => WebStrings.NotificationFieldSignInRequiredMessage,
        "send_sync_failed" => WebStrings.NotificationFieldSendSyncFailed,
        "sync_failed_message" => WebStrings.NotificationFieldSyncFailedMessage,
        "send_sync_restored" => WebStrings.NotificationFieldSendSyncRestored,
        "sync_restored_message" => WebStrings.NotificationFieldSyncRestoredMessage,
        "priority" => WebStrings.NotificationFieldPriority,
        "tags" => WebStrings.NotificationFieldTags,
        "title" => WebStrings.NotificationFieldTitle,
        "interval" => WebStrings.NotificationFieldInterval,
        _ => null
    };

    private static string? OwnLabel(string type, string field) => (type, field) switch
    {
        ("discord", "url") => WebStrings.NotificationFieldWebhookUrl,
        ("telegram", "token") => WebStrings.NotificationTelegramToken,
        ("telegram", "chat_id") => WebStrings.NotificationTelegramChatId,
        ("gotify", "key") => WebStrings.NotificationFieldAppToken,
        ("pushover", "token") => WebStrings.NotificationFieldAppToken,
        ("pushover", "user_key") => WebStrings.NotificationPushoverUserKey,
        ("apprise", "url") => WebStrings.NotificationAppriseUrl,
        ("apprise", "urls") => WebStrings.NotificationAppriseUrls,
        ("apprise", "key") => WebStrings.NotificationAppriseKey,
        ("ntfy", "topic") => WebStrings.NotificationNtfyTopic,
        ("ntfy", "token") => WebStrings.NotificationNtfyToken,
        ("ntfy", "priority") => WebStrings.NotificationNtfyPriority,
        ("ntfy", "error_priority") => WebStrings.NotificationNtfyErrorPriority,
        ("webhook", "url") => WebStrings.NotificationFieldWebhookUrl,
        ("webhook", "send_ride_synced") => WebStrings.NotificationWebhookSendRideSynced,
        ("webhook", "send_upload_failed") => WebStrings.NotificationWebhookSendUploadFailed,
        ("webhook", "send_sign_in_required") => WebStrings.NotificationWebhookSendSignInRequired,
        ("webhook", "send_sync_failed") => WebStrings.NotificationWebhookSendSyncFailed,
        ("webhook", "send_sync_restored") => WebStrings.NotificationWebhookSendSyncRestored,
        ("webhook", "send_alive") => WebStrings.NotificationWebhookSendAlive,
        ("webhook", "interval") => WebStrings.NotificationWebhookInterval,
        _ => null
    };

    private static string? CommonPlaceholder(string field) => field switch
    {
        "ride_synced_message" => WebStrings.NotificationExampleRideSyncedMessage,
        "upload_failed_message" => WebStrings.NotificationExampleUploadFailedMessage,
        "sign_in_required_message" => WebStrings.NotificationExampleSignInRequiredMessage,
        "sync_failed_message" => WebStrings.NotificationExampleSyncFailedMessage,
        "sync_restored_message" => WebStrings.NotificationExampleSyncRestoredMessage,
        _ => null
    };

    private static string? OwnPlaceholder(string type, string field) => (type, field) switch
    {
        ("discord", "url") => "https://discord.com/api/webhooks/...",
        ("discord", "display_name") => ProjectInfo.Name,
        ("gotify", "key") => WebStrings.NotificationExampleAppToken,
        ("pushover", "token") => WebStrings.NotificationExampleApiToken,
        ("pushover", "user_key") => WebStrings.NotificationExampleUserKey,
        ("telegram", "token") => WebStrings.NotificationExampleBotToken,
        ("telegram", "chat_id") => WebStrings.NotificationExampleChatId,
        ("apprise", "url") => "http://apprise:8000",
        ("apprise", "urls") => "discord://id/token, mailto://user:pass@example.com",
        ("apprise", "key") => "apprise",
        ("apprise", "tags") => WebStrings.NotificationExampleAppriseTags,
        ("apprise", "title") => ProjectInfo.Name,
        ("ntfy", "url") => "https://ntfy.sh",
        ("ntfy", "topic") => "ebike-manager",
        ("ntfy", "token") => WebStrings.NotificationExampleNtfyToken,
        ("ntfy", "tags") => "bike,warning",
        ("ntfy", "title") => ProjectInfo.Name,
        ("webhook", "url") => "http://homeassistant.local:8123/api/webhook/ebike-manager",
        _ => null
    };

    private static string Humanize(string field)
    {
        var words = field.Replace('_', ' ');
        return words.Length == 0 ? field : char.ToUpperInvariant(words[0]) + words[1..];
    }
}
