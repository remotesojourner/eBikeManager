namespace EBikeManager.Application.Models;

public sealed record FitSummary(
    DateTime StartTime,
    DateTime EndTime,
    double? TimerSeconds,
    double? DistanceMeters,
    double? CaloriesKcal,
    int? AverageHeartRate,
    int? AveragePowerWatts,
    int LapCount,
    bool HasGps);
