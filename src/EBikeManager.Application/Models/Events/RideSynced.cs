using EBikeManager.Application.Models.Dtos;

namespace EBikeManager.Application.Models.Events;

public sealed record RideSynced(RideDto Ride) : NotificationEvent;
