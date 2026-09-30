namespace EBikeManager.Application.Models.Dtos;

public sealed record AssistModeDto(string Name, int? Slot, double? ReachableRangeKm, string? Color);
