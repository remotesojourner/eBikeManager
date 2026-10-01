using EBikeManager.Application.Enums;

namespace EBikeManager.Application.Models;

public sealed record BridgeSensor(int Slot, BridgeReading Reading);
