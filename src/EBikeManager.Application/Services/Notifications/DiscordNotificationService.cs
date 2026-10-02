using EBikeManager.Application.Enums;
using EBikeManager.Application.Models;
using EBikeManager.Application.Models.Dtos;
using EBikeManager.Application.Resources;
using EBikeManager.Application.Utils;

namespace EBikeManager.Application.Services.Notifications;

internal sealed class DiscordNotificationService : MessageNotificationService
{
    private const int Green = 4572762;
    private const int Red = 12993861;
    private const int Amber = 16098851;

    public DiscordNotificationService(IHttpClientFactory httpClientFactory) : base(httpClientFactory)
    {
    }

    public override string Name => "discord";

    protected override string Title => "Discord";

    protected override string Description => "Send new rides and alerts to a Discord channel through a webhook";

    protected override IReadOnlyList<NotificationFieldSchemaDto> OwnFields { get; } =
    [
        new() { Name = "url", Type = "text", Required = true, Regex = @"https://.*discord\.com/api/webhooks/\d+/.+", Placeholder = "Webhook URL" },
        new() { Name = "display_name", Type = "text", Placeholder = ProjectInfo.Name }
    ];

    protected override MessageTemplates Templates => MessageTemplates.DiscordMarkdown;

    protected override string? SettingsProblem(NotificationSettings settings) =>
        string.IsNullOrEmpty(settings.GetString("url")) ? ApplicationStrings.WebhookUrlMissing : null;

    protected override Task<NotificationResult> SendMessageAsync(OutgoingMessage message, NotificationSettings settings, CancellationToken cancellationToken)
    {
        var payload = new
        {
            username = settings.GetString("display_name", ProjectInfo.Name),
            embeds = new[]
            {
                new
                {
                    description = message.Text,
                    color = Colour(message.Kind),
                    footer = new { text = ProjectInfo.Name },
                    timestamp = DateTime.UtcNow.ToString("o")
                }
            }
        };

        return SendAsync(JsonPost(settings.GetString("url"), payload), cancellationToken);
    }

    private static int Colour(MessageKind kind) => kind switch
    {
        MessageKind.RideSynced or MessageKind.SyncRestored => Green,
        MessageKind.SignInRequired => Amber,
        _ => Red
    };
}
