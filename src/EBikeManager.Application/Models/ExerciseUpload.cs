namespace EBikeManager.Application.Models;

public sealed record ExerciseUpload(
    string DataPointId,
    DateTime StartTime,
    DateTime EndTime,
    TimeSpan StartOffset,
    TimeSpan EndOffset,
    double? ActiveSeconds,
    double? CaloriesKcal,
    double? DistanceMeters,
    double? ElevationGainMeters,
    double? AverageSpeedKmh,
    double? AverageHeartRate,
    bool HasGps,
    string Notes,
    string? DeviceName);
