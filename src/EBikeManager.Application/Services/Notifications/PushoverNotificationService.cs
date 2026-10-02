using EBikeManager.Application.Models;
using EBikeManager.Application.Models.Dtos;
using EBikeManager.Application.Resources;
using EBikeManager.Application.Utils;

namespace EBikeManager.Application.Services.Notifications;

internal sealed class PushoverNotificationService : MessageNotificationService
{
    public PushoverNotificationService(IHttpClientFactory httpClientFactory) : base(httpClientFactory)
    {
    }

    public override string Name => "pushover";

    protected override string Title => "Pushover";

    protected override string Description => "Send push notifications through the Pushover API";

    protected override IReadOnlyList<NotificationFieldSchemaDto> OwnFields { get; } =
    [
        new() { Name = "token", Type = "text", Required = true, Regex = @"^[a-z0-9]{30}$", Placeholder = "API Token" },
        new() { Name = "user_key", Type = "text", Required = true, Regex = @"^[a-z0-9]{30}$", Placeholder = "User Key" }
    ];

    protected override MessageTemplates Templates => MessageTemplates.PlainText;

    protected override string? SettingsProblem(NotificationSettings settings) =>
        string.IsNullOrEmpty(settings.GetString("token")) || string.IsNullOrEmpty(settings.GetString("user_key"))
            ? ApplicationStrings.PushoverSettingsMissing
            : null;

    protected override Task<NotificationResult> SendMessageAsync(OutgoingMessage message, NotificationSettings settings, CancellationToken cancellationToken)
    {
        var payload = new { token = settings.GetString("token"), user = settings.GetString("user_key"), message = message.Text };
        return SendAsync(JsonPost("https://api.pushover.net/1/messages.json", payload), cancellationToken);
    }
}
