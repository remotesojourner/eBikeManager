namespace EBikeManager.Application.Models.Events;

public sealed record SyncFailed(string Error) : NotificationEvent;
