using EBikeManager.Application.Enums;
using EBikeManager.Application.Models;
using EBikeManager.Application.Models.Dtos;
using EBikeManager.Application.Services.Interfaces;
using EBikeManager.Application.Utils;

namespace EBikeManager.Application.Services;

public sealed class BridgeStateService
{
    private readonly IAppEventService _events;
    private readonly TimeProvider _time;
    private readonly Lock _lock = new();
    private BridgeSnapshot _snapshot = BridgeSnapshot.Off;

    public BridgeStateService(IAppEventService events, TimeProvider time)
    {
        _events = events;
        _time = time;
    }

    public BridgeSnapshot Snapshot
    {
        get
        {
            lock (_lock) return _snapshot;
        }
    }

    internal void SetOff() => Publish(_ => BridgeSnapshot.Off);

    internal void SetConnecting(string address) =>
        Publish(_ => new BridgeSnapshot(new BridgeStatus(BridgeConnectionState.Connecting, address, null, null, false, Now), []));

    internal void SetFailed(string address, string problem) =>
        Publish(_ => new BridgeSnapshot(new BridgeStatus(BridgeConnectionState.Failed, address, problem, null, false, Now), []));

    internal void SetConnected(string address, EsphomeDeviceInfo device, bool isDual, Func<int, string?> bikeIdFor)
    {
        IReadOnlyList<int> slots = isDual ? [1, 2] : [1];
        Publish(_ => new BridgeSnapshot(
            new BridgeStatus(BridgeConnectionState.Connected, address, null, device, isDual, Now),
            [.. slots.Select(slot => BridgeReadingsDto.Empty(slot, bikeIdFor(slot)))]));
    }

    internal BridgeReadingsDto? Apply(int slot, BridgeReading reading, float? number, bool? flag)
    {
        BridgeReadingsDto? updated = null;
        Publish(current =>
        {
            if (current.Slot(slot) is not { } readings) return current;

            updated = BridgeSensors.Apply(readings, reading, number, flag);
            return current with { Slots = [.. current.Slots.Select(existing => existing.Slot == slot ? updated : existing)] };
        });
        return updated;
    }

    private DateTime Now => _time.GetUtcNow().UtcDateTime;

    private void Publish(Func<BridgeSnapshot, BridgeSnapshot> change)
    {
        BridgeSnapshot snapshot;
        lock (_lock)
        {
            snapshot = change(_snapshot);
            if (ReferenceEquals(snapshot, _snapshot)) return;
            _snapshot = snapshot;
        }

        _events.PublishBridgeChanged(snapshot);
    }
}
