using EBikeManager.Application.Models;
using EBikeManager.Application.Models.Dtos;
using EBikeManager.Application.Models.Events;

namespace EBikeManager.Application.Services.Notifications.Interfaces;

internal interface INotificationTypeService
{
    string Name { get; }

    NotificationTypeSchemaDto Schema { get; }

    Task<NotificationResult> HandleAsync(NotificationEvent notificationEvent, NotificationContext context, CancellationToken cancellationToken);

    Task<NotificationResult> SendTestAsync(NotificationContext context, RideDto sample, CancellationToken cancellationToken);
}
