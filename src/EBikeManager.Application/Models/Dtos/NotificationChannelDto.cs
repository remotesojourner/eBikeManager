namespace EBikeManager.Application.Models.Dtos;

public sealed record NotificationChannelDto(
    string Id,
    string Type,
    string DisplayName,
    IReadOnlyDictionary<string, object?> Data,
    DateTime? LastActivity,
    bool ActivityFailed);
