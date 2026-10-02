using EBikeManager.Application.Enums;

namespace EBikeManager.Application.Models;

public sealed record NotificationContext(string Id, NotificationSettings Settings, UnitSystem Units, DateTime LocalNow);
