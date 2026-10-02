using EBikeManager.Application.Models;
using EBikeManager.Application.Models.Dtos;
using EBikeManager.Application.Resources;
using EBikeManager.Application.Utils;

namespace EBikeManager.Application.Services.Notifications;

internal sealed class NtfyNotificationService : MessageNotificationService
{
    private const string DefaultServer = "https://ntfy.sh";

    public NtfyNotificationService(IHttpClientFactory httpClientFactory) : base(httpClientFactory)
    {
    }

    public override string Name => "ntfy";

    protected override string Title => "ntfy";

    protected override string Description => "Send push notifications through ntfy.sh or your own ntfy server";

    protected override IReadOnlyList<NotificationFieldSchemaDto> OwnFields { get; } =
    [
        new() { Name = "url", Type = "text", Required = true, Regex = @"^https?://.+", Default = DefaultServer },
        new() { Name = "topic", Type = "text", Required = true, Regex = @"^[A-Za-z0-9_\-]{1,64}$", Placeholder = "ebike-manager" },
        new() { Name = "token", Type = "text" },
        new() { Name = "title", Type = "text", Default = ProjectInfo.Name },
        new() { Name = "tags", Type = "text" },
        new() { Name = "priority", Type = "text", Regex = @"^[1-5]$", Default = "3" },
        new() { Name = "error_priority", Type = "text", Regex = @"^[1-5]$", Default = "5" }
    ];

    protected override MessageTemplates Templates => MessageTemplates.PlainText;

    protected override string? SettingsProblem(NotificationSettings settings) =>
        string.IsNullOrEmpty(settings.GetString("topic")) ? ApplicationStrings.NtfyTopicMissing : null;

    protected override Task<NotificationResult> SendMessageAsync(OutgoingMessage message, NotificationSettings settings, CancellationToken cancellationToken)
    {
        var serverUrl = settings.GetString("url", DefaultServer).TrimEnd('/');
        var request = TextPost($"{serverUrl}/{settings.GetString("topic")}", message.Text);

        var priority = IsProblem(message.Kind) ? settings.GetString("error_priority", "5") : settings.GetString("priority", "3");
        request.Headers.Add("Priority", priority);

        AddHeaderWhenSet(request, "Title", settings.GetString("title"));
        AddHeaderWhenSet(request, "Tags", settings.GetString("tags"));
        if (settings.GetString("token") is { Length: > 0 } token) request.Headers.Add("Authorization", $"Bearer {token}");

        return SendAsync(request, cancellationToken);
    }

    private static void AddHeaderWhenSet(HttpRequestMessage request, string header, string value)
    {
        if (!string.IsNullOrEmpty(value)) request.Headers.Add(header, value);
    }
}
