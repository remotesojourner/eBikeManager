namespace EBikeManager.Application.Services;

public sealed class NotificationStateService
{
    private readonly Lock _lock = new();
    private readonly HashSet<string> _signInsNeeded = new(StringComparer.Ordinal);
    private bool _syncFailing;

    public bool SyncFailed()
    {
        lock (_lock)
        {
            if (_syncFailing) return false;

            _syncFailing = true;
            return true;
        }
    }

    public bool SyncWorked()
    {
        lock (_lock)
        {
            if (!_syncFailing) return false;

            _syncFailing = false;
            return true;
        }
    }

    public bool SignInNeeded(string service)
    {
        lock (_lock) return _signInsNeeded.Add(service);
    }

    public void SignedIn(string service)
    {
        lock (_lock) _signInsNeeded.Remove(service);
    }
}
