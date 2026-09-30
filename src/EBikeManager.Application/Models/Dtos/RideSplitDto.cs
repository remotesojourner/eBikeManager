namespace EBikeManager.Application.Models.Dtos;

public sealed record RideSplitDto(
    int Number,
    double DistanceKm,
    int MovingSeconds,
    double? AverageSpeedKmh,
    double? ElevationGainMeters,
    double? AverageCadence,
    double? AveragePowerWatts);
