namespace EBikeManager.Application.Models;

public sealed record BoschActivity(
    string Id,
    string? BikeId,
    string? Title,
    DateTime StartTime,
    DateTime? EndTime,
    string? TimeZone,
    int? DistanceMeters,
    int? MovingSeconds,
    double? CaloriesKcal,
    int? ElevationGainMeters,
    double? AverageSpeedKmh,
    int? RiderEnergySharePercent,
    double? AverageRiderPowerWatts,
    string AttributesJson);
