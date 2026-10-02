using System.Globalization;
using EBikeManager.Application.Enums;
using EBikeManager.Application.Models;
using EBikeManager.Application.Models.Dtos;
using EBikeManager.Application.Resources;
using EBikeManager.Application.Utils;

namespace EBikeManager.Application.Services.Notifications;

internal sealed class GotifyNotificationService : MessageNotificationService
{
    private const int DefaultPriority = 5;
    private const int FailedPriority = 8;
    private const int SignInPriority = 7;

    public GotifyNotificationService(IHttpClientFactory httpClientFactory) : base(httpClientFactory)
    {
    }

    public override string Name => "gotify";

    protected override string Title => "Gotify";

    protected override string Description => "Send new rides and alerts to a Gotify server";

    protected override IReadOnlyList<NotificationFieldSchemaDto> OwnFields { get; } =
    [
        new() { Name = "url", Type = "text", Required = true, Regex = @"https?://.+", Placeholder = "https://gotify.example.com" },
        new() { Name = "key", Type = "text", Required = true, Regex = @"^.{15}$", Placeholder = "App Token" },
        new() { Name = "priority", Type = "text", Required = true, Regex = @"^[0-9]$", Default = "5" }
    ];

    protected override MessageTemplates Templates => MessageTemplates.PlainText;

    protected override string? SettingsProblem(NotificationSettings settings) =>
        string.IsNullOrEmpty(ServerUrl(settings)) || string.IsNullOrEmpty(settings.GetString("key"))
            ? ApplicationStrings.GotifySettingsMissing
            : null;

    protected override Task<NotificationResult> SendMessageAsync(OutgoingMessage message, NotificationSettings settings, CancellationToken cancellationToken)
    {
        var priority = message.Kind switch
        {
            MessageKind.SyncFailed or MessageKind.UploadFailed => FailedPriority,
            MessageKind.SignInRequired => SignInPriority,
            _ => int.TryParse(settings.GetString("priority", DefaultPriority.ToString(CultureInfo.InvariantCulture)), NumberStyles.Integer, CultureInfo.InvariantCulture, out var configured)
                ? configured
                : DefaultPriority
        };

        var request = JsonPost($"{ServerUrl(settings)}/message", new { message = message.Text, priority });
        request.Headers.Add("Authorization", $"Bearer {settings.GetString("key")}");
        return SendAsync(request, cancellationToken);
    }

    private static string ServerUrl(NotificationSettings settings) => settings.GetString("url").TrimEnd('/');
}
