using EBikeManager.Application.Models;

namespace EBikeManager.Web.Services;

public sealed class SyncStatusStateService
{
    public SyncStatus Status { get; private set; } = SyncStatus.Idle;

    public event Action? OnChange;

    public event Action<SyncStatus>? Finished;

    public void Update(SyncStatus status)
    {
        var finished = Status.IsRunning && !status.IsRunning;
        Status = status;
        OnChange?.Invoke();
        if (finished) Finished?.Invoke(status);
    }
}
