using EBikeManager.Application.Models;
using EBikeManager.Application.Models.Dtos;
using EBikeManager.Application.Resources;
using EBikeManager.Application.Utils;

namespace EBikeManager.Application.Services.Notifications;

internal sealed class TelegramNotificationService : MessageNotificationService
{
    public TelegramNotificationService(IHttpClientFactory httpClientFactory) : base(httpClientFactory)
    {
    }

    public override string Name => "telegram";

    protected override string Title => "Telegram";

    protected override string Description => "Send new rides and alerts through a Telegram bot";

    protected override IReadOnlyList<NotificationFieldSchemaDto> OwnFields { get; } =
    [
        new() { Name = "token", Type = "text", Required = true, Regex = @"(\d+):[a-zA-Z0-9_-]+", Placeholder = "Bot Token" },
        new() { Name = "chat_id", Type = "text", Required = true, Regex = @"-?\d+", Placeholder = "Chat ID" }
    ];

    protected override MessageTemplates Templates => MessageTemplates.TelegramMarkdown;

    protected override string? SettingsProblem(NotificationSettings settings) =>
        string.IsNullOrEmpty(settings.GetString("token")) || string.IsNullOrEmpty(settings.GetString("chat_id"))
            ? ApplicationStrings.TelegramSettingsMissing
            : null;

    protected override Task<NotificationResult> SendMessageAsync(OutgoingMessage message, NotificationSettings settings, CancellationToken cancellationToken)
    {
        var payload = new { chat_id = settings.GetString("chat_id"), text = message.Text, parse_mode = "markdown" };
        return SendAsync(JsonPost($"https://api.telegram.org/bot{settings.GetString("token")}/sendMessage", payload), cancellationToken);
    }
}
