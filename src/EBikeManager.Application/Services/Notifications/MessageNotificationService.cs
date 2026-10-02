using EBikeManager.Application.Enums;
using EBikeManager.Application.Models;
using EBikeManager.Application.Models.Dtos;
using EBikeManager.Application.Models.Events;
using EBikeManager.Application.Utils;

namespace EBikeManager.Application.Services.Notifications;

internal abstract class MessageNotificationService : HttpNotificationService
{
    private static readonly IReadOnlyList<NotificationFieldSchemaDto> _messageFields =
    [
        new() { Name = "send_ride_synced", Type = "boolean", Default = true },
        new() { Name = "ride_synced_message", Type = "textarea" },
        new() { Name = "send_upload_failed", Type = "boolean", Default = true },
        new() { Name = "upload_failed_message", Type = "textarea" },
        new() { Name = "send_sign_in_required", Type = "boolean", Default = true },
        new() { Name = "sign_in_required_message", Type = "textarea" },
        new() { Name = "send_sync_failed", Type = "boolean", Default = true },
        new() { Name = "sync_failed_message", Type = "textarea" },
        new() { Name = "send_sync_restored", Type = "boolean", Default = true },
        new() { Name = "sync_restored_message", Type = "textarea" }
    ];

    protected MessageNotificationService(IHttpClientFactory httpClientFactory) : base(httpClientFactory)
    {
    }

    protected abstract string Title { get; }

    protected abstract string Description { get; }

    protected abstract IReadOnlyList<NotificationFieldSchemaDto> OwnFields { get; }

    protected abstract MessageTemplates Templates { get; }

    public override NotificationTypeSchemaDto Schema => new()
    {
        Name = Name,
        Title = Title,
        Description = Description,
        Fields = [.. OwnFields, .. _messageFields]
    };

    public override async Task<NotificationResult> HandleAsync(NotificationEvent notificationEvent, NotificationContext context, CancellationToken cancellationToken)
    {
        MessageKind? kind = notificationEvent switch
        {
            RideSynced => MessageKind.RideSynced,
            UploadFailed => MessageKind.UploadFailed,
            SignInRequired => MessageKind.SignInRequired,
            SyncFailed => MessageKind.SyncFailed,
            SyncRestored => MessageKind.SyncRestored,
            _ => null
        };

        if (kind is not { } messageKind || !context.Settings.GetBool(ToggleKey(messageKind), true)) return NotificationResult.NotApplicable;
        if (SettingsProblem(context.Settings) is { } problem) return NotificationResult.Failed(problem);

        return await SendMessageAsync(Render(messageKind, notificationEvent, context), context.Settings, cancellationToken);
    }

    public override async Task<NotificationResult> SendTestAsync(NotificationContext context, RideDto sample, CancellationToken cancellationToken)
    {
        if (SettingsProblem(context.Settings) is { } problem) return NotificationResult.Failed(problem);

        return await SendMessageAsync(Render(MessageKind.RideSynced, new RideSynced(sample), context), context.Settings, cancellationToken);
    }

    protected abstract string? SettingsProblem(NotificationSettings settings);

    protected abstract Task<NotificationResult> SendMessageAsync(OutgoingMessage message, NotificationSettings settings, CancellationToken cancellationToken);

    protected static bool IsProblem(MessageKind kind) => kind is MessageKind.SyncFailed or MessageKind.UploadFailed or MessageKind.SignInRequired;

    private OutgoingMessage Render(MessageKind kind, NotificationEvent notificationEvent, NotificationContext context)
    {
        var template = context.Settings.GetString(TemplateKey(kind), Templates.For(kind));
        return new OutgoingMessage(kind, TemplateHelper.ReplaceVariables(template, TemplateVariables.For(notificationEvent, context.Units), context.LocalNow));
    }

    private static string ToggleKey(MessageKind kind) => kind switch
    {
        MessageKind.RideSynced => "send_ride_synced",
        MessageKind.UploadFailed => "send_upload_failed",
        MessageKind.SignInRequired => "send_sign_in_required",
        MessageKind.SyncFailed => "send_sync_failed",
        _ => "send_sync_restored"
    };

    private static string TemplateKey(MessageKind kind) => kind switch
    {
        MessageKind.RideSynced => "ride_synced_message",
        MessageKind.UploadFailed => "upload_failed_message",
        MessageKind.SignInRequired => "sign_in_required_message",
        MessageKind.SyncFailed => "sync_failed_message",
        _ => "sync_restored_message"
    };
}
