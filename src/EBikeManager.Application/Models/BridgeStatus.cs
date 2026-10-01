using EBikeManager.Application.Enums;

namespace EBikeManager.Application.Models;

public sealed record BridgeStatus(BridgeConnectionState State, string? Address, string? Problem, EsphomeDeviceInfo? Device, bool IsDual, DateTime? Since)
{
    public static BridgeStatus Off { get; } = new(BridgeConnectionState.Off, null, null, null, false, null);
}
