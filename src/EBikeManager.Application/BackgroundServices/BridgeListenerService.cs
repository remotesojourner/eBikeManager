using System.Net.Sockets;
using EBikeManager.Application.Configuration;
using EBikeManager.Application.Exceptions;
using EBikeManager.Application.Models;
using EBikeManager.Application.Repositories.Interfaces;
using EBikeManager.Application.Resources;
using EBikeManager.Application.Services;
using EBikeManager.Application.Services.Interfaces;
using EBikeManager.Application.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EBikeManager.Application.BackgroundServices;

internal sealed partial class BridgeListenerService : BackgroundService
{
    private static readonly TimeSpan _longestRetry = TimeSpan.FromMinutes(5);

    private readonly IServiceScopeFactory _scopes;
    private readonly IAppEventService _events;
    private readonly BridgeStateService _state;
    private readonly TimeProvider _time;
    private readonly ILogger<BridgeListenerService> _logger;
    private CancellationTokenSource _settingsChanged = new();
    private string? _lastProblem;

    public BridgeListenerService(IServiceScopeFactory scopes, IAppEventService events, BridgeStateService state, TimeProvider time, ILogger<BridgeListenerService> logger)
    {
        _scopes = scopes;
        _events = events;
        _state = state;
        _time = time;
        _logger = logger;
    }

    public TimeSpan FirstRetry { get; set; } = TimeSpan.FromSeconds(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _events.BridgeSettingsChanged += OnSettingsChanged;
        try
        {
            var retry = FirstRetry;
            while (!stoppingToken.IsCancellationRequested)
            {
                var changed = _settingsChanged.Token;
                try
                {
                    retry = await RunOnceAsync(retry, stoppingToken, changed);
                }
                catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
                {
                    LogLoopFailed(ex);
                    await BackgroundDelay.WaitUntilAsync(Now + TimeSpan.FromMinutes(1), _time, stoppingToken, changed);
                }
            }
        }
        finally
        {
            _events.BridgeSettingsChanged -= OnSettingsChanged;
            _state.SetOff();
        }
    }

    public override void Dispose()
    {
        _settingsChanged.Dispose();
        base.Dispose();
    }

    private async Task<TimeSpan> RunOnceAsync(TimeSpan retry, CancellationToken stoppingToken, CancellationToken changed)
    {
        if (await LoadAsync(stoppingToken) is not { } config)
        {
            _state.SetOff();
            _lastProblem = null;
            await BackgroundDelay.WaitUntilAsync(DateTime.MaxValue, _time, stoppingToken, changed);
            return FirstRetry;
        }

        var connectedFor = await RunSessionAsync(config, stoppingToken, changed);
        if (stoppingToken.IsCancellationRequested || changed.IsCancellationRequested) return FirstRetry;
        if (connectedFor >= _longestRetry) retry = FirstRetry;

        await BackgroundDelay.WaitUntilAsync(Now + retry, _time, stoppingToken, changed);
        return TimeSpan.FromTicks(Math.Min(retry.Ticks * 2, _longestRetry.Ticks));
    }

    private async Task<TimeSpan> RunSessionAsync(BridgeConfig config, CancellationToken stoppingToken, CancellationToken changed)
    {
        using var stop = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken, changed);
        var session = new BridgeSession(config, _state, _scopes, _time, _logger);
        try
        {
            await session.RunAsync(stop.Token);
        }
        catch (OperationCanceledException) when (stop.IsCancellationRequested)
        {
        }
        catch (BridgeConnectionException ex)
        {
            Fail(config, ex.Message);
        }
        catch (Exception ex) when (ex is IOException or SocketException or InvalidDataException)
        {
            Fail(config, ApplicationStrings.Format(ApplicationStrings.BridgeLost, ex.Message));
        }

        if (session.ConnectedAt != null) _lastProblem = null;
        return session.ConnectedAt is { } since ? Now - since : TimeSpan.Zero;
    }

    private void Fail(BridgeConfig config, string problem)
    {
        _state.SetFailed(config.Address, problem);
        if (problem == _lastProblem) return;

        _lastProblem = problem;
        LogConnectionFailed(config.Address, problem);
    }

    private async Task<BridgeConfig?> LoadAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopes.CreateScope();
        var bridge = (await scope.ServiceProvider.GetRequiredService<ISettingsRepository>().GetAsync(cancellationToken)).Bridge;
        if (bridge.Address is not { } address || !BridgeSettings.TryParseAddress(address, out var host, out var port)) return null;

        var keyText = await scope.ServiceProvider.GetRequiredService<SecretStoreService>().GetAsync(SecretStoreService.BridgeEncryptionKey, cancellationToken);
        var key = BridgeSettings.TryReadKey(keyText, out var parsed) ? parsed : null;
        var bikes = await scope.ServiceProvider.GetRequiredService<IBikeRepository>().GetAllAsync(cancellationToken);
        var firstBike = bridge.FirstBikeId ?? (bikes.Count == 1 ? bikes[0].Id : null);
        return new BridgeConfig(address, host, port, key, firstBike, bridge.SecondBikeId);
    }

    private void OnSettingsChanged() => Interlocked.Exchange(ref _settingsChanged, new CancellationTokenSource()).Cancel();

    private DateTime Now => _time.GetUtcNow().UtcDateTime;

    [LoggerMessage(Level = LogLevel.Warning, Message = "Could not use the eBike bridge {Address}: {Problem}")]
    private partial void LogConnectionFailed(string address, string problem);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error in the eBike bridge loop")]
    private partial void LogLoopFailed(Exception exception);
}
