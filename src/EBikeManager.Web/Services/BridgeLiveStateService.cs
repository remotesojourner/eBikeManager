using EBikeManager.Application.Models;

namespace EBikeManager.Web.Services;

public sealed class BridgeLiveStateService
{
    public BridgeSnapshot Snapshot { get; private set; } = BridgeSnapshot.Off;

    public event Action? OnChange;

    public void Update(BridgeSnapshot snapshot)
    {
        Snapshot = snapshot;
        OnChange?.Invoke();
    }
}
