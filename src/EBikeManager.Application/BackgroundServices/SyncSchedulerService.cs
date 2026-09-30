using EBikeManager.Application.Configuration;
using EBikeManager.Application.Repositories.Interfaces;
using EBikeManager.Application.Services;
using EBikeManager.Application.Services.Interfaces;
using EBikeManager.Application.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EBikeManager.Application.BackgroundServices;

internal sealed partial class SyncSchedulerService : BackgroundService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly IAppEventService _events;
    private readonly TimeProvider _time;
    private readonly ILogger<SyncSchedulerService> _logger;
    private CancellationTokenSource _scheduleChanged = new();

    public SyncSchedulerService(IServiceScopeFactory scopes, IAppEventService events, TimeProvider time, ILogger<SyncSchedulerService> logger)
    {
        _scopes = scopes;
        _events = events;
        _time = time;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _events.SettingsChanged += OnSettingsChanged;
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    if (await WaitForNextRunAsync(stoppingToken)) await RunAsync(stoppingToken);
                }
                catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
                {
                    LogLoopFailed(ex);
                    await BackgroundDelay.WaitAsync(TimeSpan.FromMinutes(1), _time, stoppingToken);
                }
            }
        }
        finally
        {
            _events.SettingsChanged -= OnSettingsChanged;
        }
    }

    public override void Dispose()
    {
        _scheduleChanged.Dispose();
        base.Dispose();
    }

    private async Task<bool> WaitForNextRunAsync(CancellationToken stoppingToken)
    {
        var scheduleChanged = _scheduleChanged.Token;
        using var scope = _scopes.CreateScope();
        var schedule = (await scope.ServiceProvider.GetRequiredService<ISettingsRepository>().GetAsync(stoppingToken)).Schedule;
        var due = schedule.NextRunAfter(_time.GetUtcNow().UtcDateTime) ?? DateTime.MaxValue;
        return await BackgroundDelay.WaitUntilAsync(due, _time, stoppingToken, scheduleChanged);
    }

    private async Task RunAsync(CancellationToken stoppingToken)
    {
        using var scope = _scopes.CreateScope();
        await scope.ServiceProvider.GetRequiredService<SyncRunService>().RunScheduledAsync(stoppingToken);
    }

    private void OnSettingsChanged(IReadOnlyDictionary<string, string> changes)
    {
        if (changes.ContainsKey(SettingDefinitions.SyncCron)) Interlocked.Exchange(ref _scheduleChanged, new CancellationTokenSource()).Cancel();
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Error in the sync scheduler loop")]
    private partial void LogLoopFailed(Exception exception);
}
