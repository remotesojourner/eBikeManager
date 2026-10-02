using EBikeManager.Application.Models.Dtos;

namespace EBikeManager.Application.Models.Events;

public sealed record UploadFailed(RideDto Ride, string Service, string Error) : NotificationEvent;
