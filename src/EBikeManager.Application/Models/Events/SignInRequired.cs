namespace EBikeManager.Application.Models.Events;

public sealed record SignInRequired(string Service) : NotificationEvent;
