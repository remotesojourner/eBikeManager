namespace EBikeManager.Application.Models.Dtos;

public sealed record BikeBatteryDto(
    string? ProductName,
    double? CapacityWh,
    double? LevelPercent,
    double? RemainingWh,
    double? ChargeCycles,
    double? ChargeCyclesOnBike,
    double? ChargeCyclesOffBike,
    double? LifetimeDeliveredWh,
    bool? Charging,
    bool? ChargerConnected,
    string? SoftwareVersion,
    string? SerialNumber);
