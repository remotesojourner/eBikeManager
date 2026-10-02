using EBikeManager.Application.Enums;

namespace EBikeManager.Application.Models;

public sealed record NotificationResult(NotificationOutcome Outcome, string? Error = null)
{
    public static NotificationResult Sent { get; } = new(NotificationOutcome.Sent);

    public static NotificationResult NotApplicable { get; } = new(NotificationOutcome.NotApplicable);

    public static NotificationResult Failed(string error) => new(NotificationOutcome.Failed, error);
}
