namespace EBikeManager.Application.Models;

public sealed record SyncStatus(bool IsRunning, string? Progress, DateTime? LastFinishedAt, SyncRunResult? LastResult, string? LastError)
{
    public static SyncStatus Idle { get; } = new(false, null, null, null, null);
}
