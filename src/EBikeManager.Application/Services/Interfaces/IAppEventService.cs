using EBikeManager.Application.Models;

namespace EBikeManager.Application.Services.Interfaces;

public interface IAppEventService
{
    event Action<IReadOnlyDictionary<string, string>>? SettingsChanged;

    event Action<SyncStatus>? SyncStatusChanged;

    event Action? BridgeSettingsChanged;

    event Action<BridgeSnapshot>? BridgeChanged;

    void PublishSettingsChanged(IReadOnlyDictionary<string, string> changes);

    void PublishSyncStatusChanged(SyncStatus status);

    void PublishBridgeSettingsChanged();

    void PublishBridgeChanged(BridgeSnapshot snapshot);
}
