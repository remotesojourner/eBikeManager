using EBikeManager.Application.Enums;
using EBikeManager.Application.Models.Dtos;

namespace EBikeManager.Application.Models;

public sealed record BridgeSnapshot(BridgeStatus Status, IReadOnlyList<BridgeReadingsDto> Slots)
{
    public static BridgeSnapshot Off { get; } = new(BridgeStatus.Off, []);

    public BridgeReadingsDto? LiveFor(string bikeId) =>
        Status.State == BridgeConnectionState.Connected ? Slots.FirstOrDefault(slot => slot.BikeId == bikeId && slot.Connected) : null;

    public BridgeReadingsDto? Slot(int slot) => Slots.FirstOrDefault(readings => readings.Slot == slot);
}
