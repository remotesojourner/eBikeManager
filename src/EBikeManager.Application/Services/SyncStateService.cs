using EBikeManager.Application.Models;
using EBikeManager.Application.Services.Interfaces;

namespace EBikeManager.Application.Services;

public sealed class SyncStateService
{
    private readonly Lock _gate = new();
    private readonly IAppEventService _events;
    private readonly TimeProvider _time;

    public SyncStateService(IAppEventService events, TimeProvider time)
    {
        _events = events;
        _time = time;
    }

    public SyncStatus Status { get; private set; } = SyncStatus.Idle;

    public bool TryStart(string progress)
    {
        lock (_gate)
        {
            if (Status.IsRunning) return false;
            Status = Status with { IsRunning = true, Progress = progress, LastError = null };
        }

        _events.PublishSyncStatusChanged(Status);
        return true;
    }

    public void ReportProgress(string progress)
    {
        lock (_gate)
        {
            if (!Status.IsRunning) return;
            Status = Status with { Progress = progress };
        }

        _events.PublishSyncStatusChanged(Status);
    }

    public void Finish(SyncRunResult? result, string? error)
    {
        lock (_gate)
        {
            Status = new SyncStatus(false, null, _time.GetUtcNow().UtcDateTime, result ?? Status.LastResult, error);
        }

        _events.PublishSyncStatusChanged(Status);
    }
}
