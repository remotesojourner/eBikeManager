using EBikeManager.Application.Enums;
using EBikeManager.Application.Models;
using EBikeManager.Application.Models.Dtos;
using EBikeManager.Application.Models.Entities;
using EBikeManager.Application.Models.Events;
using EBikeManager.Application.Repositories.Interfaces;
using EBikeManager.Application.Resources;
using EBikeManager.Application.Services.Interfaces;
using EBikeManager.Application.Services.Notifications.Interfaces;
using Microsoft.Extensions.Logging;

namespace EBikeManager.Application.Services;

internal sealed partial class NotificationDispatchService : INotificationDispatchService
{
    private readonly INotificationChannelRepository _channels;
    private readonly ISettingsRepository _settings;
    private readonly Dictionary<string, INotificationTypeService> _types;
    private readonly HeartbeatScheduleService _heartbeats;
    private readonly TimeProvider _time;
    private readonly ILogger<NotificationDispatchService> _logger;

    public NotificationDispatchService(
        INotificationChannelRepository channels,
        ISettingsRepository settings,
        IEnumerable<INotificationTypeService> types,
        HeartbeatScheduleService heartbeats,
        TimeProvider time,
        ILogger<NotificationDispatchService> logger)
    {
        _channels = channels;
        _settings = settings;
        _heartbeats = heartbeats;
        _time = time;
        _logger = logger;
        _types = types.ToDictionary(type => type.Name, StringComparer.OrdinalIgnoreCase);
        Schemas = _types.ToDictionary(pair => pair.Key, pair => pair.Value.Schema, StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyDictionary<string, NotificationTypeSchemaDto> Schemas { get; }

    public async Task PublishAsync(NotificationEvent notificationEvent, CancellationToken cancellationToken = default)
    {
        var channels = await _channels.GetAllAsync(cancellationToken);
        if (channels.Count == 0) return;

        var units = (await _settings.GetAsync(cancellationToken)).Units;
        foreach (var channel in channels)
        {
            if (NotificationSettings.Parse(channel.Data) is not { } settings)
            {
                LogUnreadableSettings(channel.Type, channel.Id);
                await SaveActivityAsync(channel.Id, true, cancellationToken);
                continue;
            }

            if (notificationEvent is Heartbeat && !_heartbeats.IsDue(channel.Id, settings.GetInt("interval", 1))) continue;

            try
            {
                var result = await HandleAsync(channel, notificationEvent, new NotificationContext(channel.Id, settings, units, _time.GetLocalNow().DateTime), cancellationToken);
                if (result.Outcome == NotificationOutcome.NotApplicable) continue;

                if (result.Outcome == NotificationOutcome.Failed) LogNotificationFailed(channel.Type, channel.Id, result.Error);
                await SaveActivityAsync(channel.Id, result.Outcome == NotificationOutcome.Failed, cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
            {
                LogDispatchFailed(ex, channel.Type, channel.Id);
                await SaveActivityAsync(channel.Id, true, cancellationToken);
            }
        }
    }

    public async Task<NotificationResult> TestAsync(string type, string id, string settingsJson, RideDto sample, CancellationToken cancellationToken = default)
    {
        if (!_types.TryGetValue(type, out var notificationType)) return UnknownType(type);
        if (NotificationSettings.Parse(settingsJson) is not { } settings) return NotificationResult.Failed(ApplicationStrings.NotificationSettingsUnreadable);

        try
        {
            var units = (await _settings.GetAsync(cancellationToken)).Units;
            return await notificationType.SendTestAsync(new NotificationContext(id, settings, units, _time.GetLocalNow().DateTime), sample, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            LogSendTestFailed(ex, type, id);
            return NotificationResult.Failed(ex.Message);
        }
    }

    private static NotificationResult UnknownType(string type) => NotificationResult.Failed(ApplicationStrings.Format(ApplicationStrings.NotificationUnknownType, type));

    private Task<NotificationResult> HandleAsync(NotificationChannel channel, NotificationEvent notificationEvent, NotificationContext context, CancellationToken cancellationToken) =>
        _types.TryGetValue(channel.Type, out var notificationType)
            ? notificationType.HandleAsync(notificationEvent, context, cancellationToken)
            : Task.FromResult(UnknownType(channel.Type));

    private Task SaveActivityAsync(string id, bool failed, CancellationToken cancellationToken) =>
        _channels.SaveActivityAsync(id, failed, _time.GetUtcNow().UtcDateTime, cancellationToken);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Notification {Type} ({Id}) was not sent because its saved settings can't be read")]
    private partial void LogUnreadableSettings(string type, string id);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Notification {Type} ({Id}) failed: {Error}")]
    private partial void LogNotificationFailed(string type, string id, string? error);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to send notification {Type} ({Id})")]
    private partial void LogDispatchFailed(Exception exception, string type, string id);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Sending a test for notification {Type} ({Id}) failed")]
    private partial void LogSendTestFailed(Exception exception, string type, string id);
}
