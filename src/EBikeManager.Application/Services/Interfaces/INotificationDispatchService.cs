using EBikeManager.Application.Models;
using EBikeManager.Application.Models.Dtos;
using EBikeManager.Application.Models.Events;

namespace EBikeManager.Application.Services.Interfaces;

public interface INotificationDispatchService
{
    IReadOnlyDictionary<string, NotificationTypeSchemaDto> Schemas { get; }

    Task PublishAsync(NotificationEvent notificationEvent, CancellationToken cancellationToken = default);

    Task<NotificationResult> TestAsync(string type, string id, string settingsJson, RideDto sample, CancellationToken cancellationToken = default);
}
