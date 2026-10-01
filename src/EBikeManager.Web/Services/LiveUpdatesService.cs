using EBikeManager.Application.Models;
using EBikeManager.Application.Services;
using EBikeManager.Application.Services.Interfaces;

namespace EBikeManager.Web.Services;

public sealed partial class LiveUpdatesService : IDisposable
{
    private readonly IAppEventService _events;
    private readonly SyncStateService _syncState;
    private readonly SyncStatusStateService _syncStatus;
    private readonly SettingsStateService _settings;
    private readonly BridgeStateService _bridgeState;
    private readonly BridgeLiveStateService _bridge;
    private readonly ILogger<LiveUpdatesService> _logger;
    private Func<Func<Task>, Task>? _dispatch;

    public LiveUpdatesService(
        IAppEventService events,
        SyncStateService syncState,
        SyncStatusStateService syncStatus,
        SettingsStateService settings,
        BridgeStateService bridgeState,
        BridgeLiveStateService bridge,
        ILogger<LiveUpdatesService> logger)
    {
        _events = events;
        _syncState = syncState;
        _syncStatus = syncStatus;
        _settings = settings;
        _bridgeState = bridgeState;
        _bridge = bridge;
        _logger = logger;
    }

    public void Start(Func<Func<Task>, Task> dispatch)
    {
        if (_dispatch != null) return;

        _dispatch = dispatch;
        _syncStatus.Update(_syncState.Status);
        _bridge.Update(_bridgeState.Snapshot);
        _events.SyncStatusChanged += OnSyncStatusChanged;
        _events.SettingsChanged += OnSettingsChanged;
        _events.BridgeChanged += OnBridgeChanged;
    }

    public void Dispose()
    {
        _events.SyncStatusChanged -= OnSyncStatusChanged;
        _events.SettingsChanged -= OnSettingsChanged;
        _events.BridgeChanged -= OnBridgeChanged;
    }

    private void OnSyncStatusChanged(SyncStatus status) => Apply(() =>
    {
        _syncStatus.Update(status);
        return Task.CompletedTask;
    });

    private void OnSettingsChanged(IReadOnlyDictionary<string, string> changes) => Apply(_settings.LoadAsync);

    private void OnBridgeChanged(BridgeSnapshot snapshot) => Apply(() =>
    {
        _bridge.Update(snapshot);
        return Task.CompletedTask;
    });

    private void Apply(Func<Task> update)
    {
        if (_dispatch is not { } dispatch) return;

        _ = RunAsync(dispatch, update);
    }

    private async Task RunAsync(Func<Func<Task>, Task> dispatch, Func<Task> update)
    {
        try
        {
            await dispatch(update);
        }
        catch (Exception ex) when (ex is InvalidOperationException or ObjectDisposedException)
        {
            LogUpdateFailed(ex);
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "A live update couldn't be applied to an open page")]
    private partial void LogUpdateFailed(Exception exception);
}
