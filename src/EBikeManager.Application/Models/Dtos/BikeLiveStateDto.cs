namespace EBikeManager.Application.Models.Dtos;

public sealed record BikeLiveStateDto(
    double? ChargePercent,
    bool? Charging,
    bool? ChargerConnected,
    double? RemainingWh,
    double? MinRangeKm,
    double? MaxRangeKm,
    double? MinutesToFull,
    DateTime? UpdatedAt);
