using System.Data.Common;
using EBikeManager.Application.Enums;
using EBikeManager.Application.Exceptions;
using EBikeManager.Application.Models;
using EBikeManager.Application.Models.Dtos;
using EBikeManager.Application.Repositories.Interfaces;
using EBikeManager.Application.Resources;
using EBikeManager.Application.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EBikeManager.Application.Services;

internal sealed partial class BridgeSession
{
    public const string ClientInfo = "eBike Manager";

    private static readonly TimeSpan _setupLimit = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan _flushLimit = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan _pingInterval = TimeSpan.FromSeconds(20);
    private static readonly TimeSpan _silenceLimit = TimeSpan.FromSeconds(90);
    private static readonly TimeSpan _odometerSaveInterval = TimeSpan.FromMinutes(1);

    private readonly BridgeConfig _config;
    private readonly BridgeStateService _state;
    private readonly IServiceScopeFactory _scopes;
    private readonly TimeProvider _time;
    private readonly ILogger _logger;
    private readonly Dictionary<int, double> _savedBattery = [];
    private readonly Dictionary<int, (string BikeId, double Kilometres)> _pendingOdometer = [];
    private readonly Dictionary<int, DateTime> _odometerSavedAt = [];
    private long _lastReceivedTicks;
    private volatile bool _silent;

    public BridgeSession(BridgeConfig config, BridgeStateService state, IServiceScopeFactory scopes, TimeProvider time, ILogger logger)
    {
        _config = config;
        _state = state;
        _scopes = scopes;
        _time = time;
        _logger = logger;
    }

    public DateTime? ConnectedAt { get; private set; }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        _state.SetConnecting(_config.Address);
        await using var connection = await WithinSetupLimitAsync(token => EsphomeConnection.OpenAsync(_config.Host, _config.Port, _config.EncryptionKey, token), cancellationToken);
        var (device, sensors) = await WithinSetupLimitAsync(token => SetUpAsync(connection, token), cancellationToken);

        ConnectedAt = Now;
        _state.SetConnected(_config.Address, device, sensors.Values.Any(sensor => sensor.Slot == 2), _config.BikeIdFor);
        LogConnected(_config.Address, device.Name, device.EsphomeVersion ?? "?");
        await connection.SendAsync(EsphomeMessageType.SubscribeStatesRequest, [], cancellationToken);

        using var session = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var keepAlive = KeepAliveAsync(connection, session);
        try
        {
            await ListenAsync(connection, sensors, session.Token);
        }
        catch (OperationCanceledException) when (_silent && !cancellationToken.IsCancellationRequested)
        {
            throw new BridgeConnectionException(ApplicationStrings.BridgeStoppedAnswering);
        }
        finally
        {
            await session.CancelAsync();
            await keepAlive;
            await FlushOdometersAsync();
        }
    }

    private async Task<(EsphomeDeviceInfo Device, Dictionary<uint, BridgeSensor> Sensors)> SetUpAsync(EsphomeConnection connection, CancellationToken cancellationToken)
    {
        await connection.SendAsync(EsphomeMessageType.HelloRequest, EsphomeMessages.HelloRequest(ClientInfo), cancellationToken);
        await connection.SendAsync(EsphomeMessageType.AuthenticationRequest, [], cancellationToken);
        await ReceiveUntilAsync(connection, EsphomeMessageType.HelloResponse, cancellationToken);

        await connection.SendAsync(EsphomeMessageType.DeviceInfoRequest, [], cancellationToken);
        var device = EsphomeMessages.DeviceInfo((await ReceiveUntilAsync(connection, EsphomeMessageType.DeviceInfoResponse, cancellationToken)).Payload);

        await connection.SendAsync(EsphomeMessageType.ListEntitiesRequest, [], cancellationToken);
        var sensors = new Dictionary<uint, BridgeSensor>();
        for (var frame = await ReceiveAsync(connection, cancellationToken); frame.Type != EsphomeMessageType.ListEntitiesDoneResponse; frame = await ReceiveAsync(connection, cancellationToken))
        {
            if (frame.Type is not (EsphomeMessageType.ListEntitiesSensorResponse or EsphomeMessageType.ListEntitiesBinarySensorResponse)) continue;
            if (EsphomeMessages.Entity(frame.Payload) is { } entity && BridgeSensors.Find(entity.Name) is { } sensor) sensors[entity.Key] = sensor;
        }

        if (sensors.Count == 0) throw new BridgeConnectionException(ApplicationStrings.BridgeNoSensors);
        return (device, sensors);
    }

    private async Task ListenAsync(EsphomeConnection connection, Dictionary<uint, BridgeSensor> sensors, CancellationToken cancellationToken)
    {
        while (true)
        {
            var frame = await ReceiveAsync(connection, cancellationToken);
            var state = frame.Type switch
            {
                EsphomeMessageType.SensorStateResponse => EsphomeMessages.SensorState(frame.Payload),
                EsphomeMessageType.BinarySensorStateResponse => EsphomeMessages.BinarySensorState(frame.Payload),
                _ => null
            };
            if (state == null || !sensors.TryGetValue(state.Key, out var sensor)) continue;

            if (_state.Apply(sensor.Slot, sensor.Reading, state.Number, state.Flag) is { } readings) await SaveAsync(readings, sensor.Reading, cancellationToken);
        }
    }

    private async Task<EsphomeFrame> ReceiveUntilAsync(EsphomeConnection connection, EsphomeMessageType type, CancellationToken cancellationToken)
    {
        while (true)
        {
            var frame = await ReceiveAsync(connection, cancellationToken);
            if (frame.Type == type) return frame;
        }
    }

    private async Task<EsphomeFrame> ReceiveAsync(EsphomeConnection connection, CancellationToken cancellationToken)
    {
        while (true)
        {
            var frame = await connection.ReceiveAsync(cancellationToken);
            Interlocked.Exchange(ref _lastReceivedTicks, Now.Ticks);
            switch (frame.Type)
            {
                case EsphomeMessageType.PingRequest:
                    await connection.SendAsync(EsphomeMessageType.PingResponse, [], cancellationToken);
                    break;
                case EsphomeMessageType.GetTimeRequest:
                    await connection.SendAsync(EsphomeMessageType.GetTimeResponse, EsphomeMessages.GetTimeResponse(_time.GetUtcNow()), cancellationToken);
                    break;
                case EsphomeMessageType.DisconnectRequest:
                    await connection.SendAsync(EsphomeMessageType.DisconnectResponse, [], cancellationToken);
                    throw new BridgeConnectionException(ApplicationStrings.BridgeClosed);
                default:
                    return frame;
            }
        }
    }

    private async Task KeepAliveAsync(EsphomeConnection connection, CancellationTokenSource session)
    {
        try
        {
            while (true)
            {
                await Task.Delay(_pingInterval, _time, session.Token);
                if (Now - new DateTime(Interlocked.Read(ref _lastReceivedTicks), DateTimeKind.Utc) > _silenceLimit)
                {
                    _silent = true;
                    await session.CancelAsync();
                    return;
                }

                await connection.SendAsync(EsphomeMessageType.PingRequest, [], session.Token);
            }
        }
        catch (OperationCanceledException) when (session.IsCancellationRequested)
        {
        }
        catch (IOException ex)
        {
            LogPingFailed(ex, _config.Address);
        }
    }

    private async Task SaveAsync(BridgeReadingsDto readings, BridgeReading reading, CancellationToken cancellationToken)
    {
        if (readings.BikeId is not { } bikeId) return;

        switch (reading)
        {
            case BridgeReading.BatteryPercent when readings.BatteryPercent is { } percent && _savedBattery.GetValueOrDefault(readings.Slot, -1) != percent:
                _savedBattery[readings.Slot] = percent;
                await WriteAsync(bikes => bikes.SaveBridgeBatteryAsync(bikeId, percent, Now, cancellationToken));
                break;
            case BridgeReading.Odometer when readings.OdometerKm is { } kilometres:
                _pendingOdometer[readings.Slot] = (bikeId, kilometres);
                if (Now - _odometerSavedAt.GetValueOrDefault(readings.Slot, DateTime.MinValue) >= _odometerSaveInterval) await FlushOdometerAsync(readings.Slot, cancellationToken);
                break;
            case BridgeReading.Connected when !readings.Connected:
                await FlushOdometerAsync(readings.Slot, cancellationToken);
                break;
        }
    }

    private async Task FlushOdometersAsync()
    {
        using var limit = new CancellationTokenSource(_flushLimit);
        foreach (var slot in _pendingOdometer.Keys.ToList()) await FlushOdometerAsync(slot, limit.Token);
    }

    private async Task FlushOdometerAsync(int slot, CancellationToken cancellationToken)
    {
        if (!_pendingOdometer.Remove(slot, out var pending)) return;

        _odometerSavedAt[slot] = Now;
        await WriteAsync(bikes => bikes.SaveBridgeOdometerAsync(pending.BikeId, pending.Kilometres, Now, cancellationToken));
    }

    private async Task WriteAsync(Func<IBikeRepository, Task> write)
    {
        try
        {
            using var scope = _scopes.CreateScope();
            await write(scope.ServiceProvider.GetRequiredService<IBikeRepository>());
        }
        catch (DbException ex)
        {
            LogSaveFailed(ex, _config.Address);
        }
    }

    private static async Task<T> WithinSetupLimitAsync<T>(Func<CancellationToken, Task<T>> step, CancellationToken cancellationToken)
    {
        using var limit = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        limit.CancelAfter(_setupLimit);
        try
        {
            return await step(limit.Token);
        }
        catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            throw new BridgeConnectionException(ApplicationStrings.BridgeNoAnswer, ex);
        }
    }

    private DateTime Now => _time.GetUtcNow().UtcDateTime;

    [LoggerMessage(Level = LogLevel.Information, Message = "Connected to the eBike bridge {Address} ({Device}, ESPHome {Version})")]
    private partial void LogConnected(string address, string device, string version);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Could not ping the eBike bridge {Address}")]
    private partial void LogPingFailed(Exception exception, string address);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Could not save a reading from the eBike bridge {Address}")]
    private partial void LogSaveFailed(Exception exception, string address);
}
