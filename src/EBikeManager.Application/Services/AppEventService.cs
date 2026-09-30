using EBikeManager.Application.Models;
using EBikeManager.Application.Services.Interfaces;

namespace EBikeManager.Application.Services;

public sealed class AppEventService : IAppEventService
{
    public event Action<IReadOnlyDictionary<string, string>>? SettingsChanged;

    public event Action<SyncStatus>? SyncStatusChanged;

    public void PublishSettingsChanged(IReadOnlyDictionary<string, string> changes) => SettingsChanged?.Invoke(changes);

    public void PublishSyncStatusChanged(SyncStatus status) => SyncStatusChanged?.Invoke(status);
}
