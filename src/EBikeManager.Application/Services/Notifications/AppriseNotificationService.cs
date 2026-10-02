using System.Net;
using System.Text.Json;
using EBikeManager.Application.Enums;
using EBikeManager.Application.Models;
using EBikeManager.Application.Models.Dtos;
using EBikeManager.Application.Resources;
using EBikeManager.Application.Utils;

namespace EBikeManager.Application.Services.Notifications;

internal sealed class AppriseNotificationService : MessageNotificationService
{
    private static readonly string[] _problemLevels = ["WARNING", "ERROR", "CRITICAL"];

    public AppriseNotificationService(IHttpClientFactory httpClientFactory) : base(httpClientFactory)
    {
    }

    public override string Name => "apprise";

    protected override string Title => "Apprise";

    protected override string Description => "Send alerts to any service Apprise supports, through an Apprise API server";

    protected override IReadOnlyList<NotificationFieldSchemaDto> OwnFields { get; } =
    [
        new() { Name = "url", Type = "text", Required = true, Regex = @"^https?://.+" },
        new() { Name = "urls", Type = "textarea" },
        new() { Name = "key", Type = "text", Regex = @"^[\w-]{1,128}$" },
        new() { Name = "tags", Type = "text" },
        new() { Name = "title", Type = "text" }
    ];

    protected override MessageTemplates Templates => MessageTemplates.PlainText;

    protected override string? SettingsProblem(NotificationSettings settings)
    {
        if (string.IsNullOrEmpty(settings.GetString("url"))) return ApplicationStrings.AppriseServerUrlMissing;

        return settings.GetString("urls").Length > 0 && settings.GetString("key").Length > 0
            ? ApplicationStrings.AppriseUrlsOrKey
            : null;
    }

    protected override Task<NotificationResult> SendMessageAsync(OutgoingMessage message, NotificationSettings settings, CancellationToken cancellationToken)
    {
        var serverUrl = settings.GetString("url").TrimEnd('/');
        var key = settings.GetString("key");
        var tags = settings.GetString("tags");

        var payload = new Dictionary<string, string>();
        AddWhenSet(payload, "urls", settings.GetString("urls"));
        AddWhenSet(payload, "tag", tags);
        AddWhenSet(payload, "title", settings.GetString("title"));
        payload["body"] = message.Text;
        payload["type"] = NotifyType(message.Kind);
        payload["format"] = "text";

        var url = key.Length == 0 ? $"{serverUrl}/notify/" : $"{serverUrl}/notify/{Uri.EscapeDataString(key)}";
        var request = JsonPost(url, payload);
        request.Headers.Add("Accept", "application/json");

        return SendAsync(request, cancellationToken, (status, reply) => status switch
        {
            HttpStatusCode.NoContent when key.Length == 0 => NotificationResult.Failed(ApplicationStrings.AppriseNoValidUrls),
            HttpStatusCode.NoContent => NotificationResult.Failed(ApplicationStrings.Format(ApplicationStrings.AppriseNoConfiguration, key)),
            _ when IsSuccess(status) => null,
            _ => NotificationResult.Failed(Explain(status, reply, tags))
        });
    }

    private string Explain(HttpStatusCode status, string reply, string tags)
    {
        if (AppriseReply.Parse(reply) is not { } parsed) return Answered(status, reply);

        if (status == HttpStatusCode.FailedDependency && parsed.Problems.Count == 0)
            return tags.Length > 0
                ? ApplicationStrings.Format(ApplicationStrings.AppriseNothingTagged, tags)
                : ApplicationStrings.AppriseNothingUntagged;

        return Answered(status, parsed.Problems.Count == 0 ? parsed.Error : $"{parsed.Error}: {string.Join("; ", parsed.Problems)}");
    }

    private static string NotifyType(MessageKind kind) => kind switch
    {
        MessageKind.RideSynced or MessageKind.SyncRestored => "success",
        MessageKind.SignInRequired => "warning",
        _ => "failure"
    };

    private static void AddWhenSet(Dictionary<string, string> payload, string name, string value)
    {
        if (value.Length > 0) payload[name] = value;
    }

    private sealed record AppriseReply(string Error, IReadOnlyList<string> Problems)
    {
        public static AppriseReply? Parse(string reply)
        {
            try
            {
                using var document = JsonDocument.Parse(reply);
                if (document.RootElement is not { ValueKind: JsonValueKind.Object } root
                    || !root.TryGetProperty("error", out var error) || error.ValueKind != JsonValueKind.String)
                    return null;

                var problems = root.TryGetProperty("details", out var details) && details.ValueKind == JsonValueKind.Array
                    ? details.EnumerateArray().Select(Problem).OfType<string>().ToList()
                    : [];

                return new AppriseReply(error.GetString()!, problems);
            }
            catch (JsonException)
            {
                return null;
            }
        }

        private static string? Problem(JsonElement logEntry) =>
            logEntry.ValueKind == JsonValueKind.Array && logEntry.GetArrayLength() == 3
            && logEntry[0].ValueKind == JsonValueKind.String && _problemLevels.Contains(logEntry[0].GetString())
            && logEntry[2].ValueKind == JsonValueKind.String
                ? logEntry[2].GetString()
                : null;
    }
}
