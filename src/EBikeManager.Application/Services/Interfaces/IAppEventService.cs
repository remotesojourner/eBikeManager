using EBikeManager.Application.Models;

namespace EBikeManager.Application.Services.Interfaces;

public interface IAppEventService
{
    event Action<IReadOnlyDictionary<string, string>>? SettingsChanged;

    event Action<SyncStatus>? SyncStatusChanged;

    void PublishSettingsChanged(IReadOnlyDictionary<string, string> changes);

    void PublishSyncStatusChanged(SyncStatus status);
}
