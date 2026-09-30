using EBikeManager.Application.Enums;
using EBikeManager.Application.Models.Entities;
using EBikeManager.Application.Utils;

namespace EBikeManager.Application.Models.Dtos;

public sealed record RideDto(
    string Id,
    string BikeId,
    string? Title,
    DateTime StartTime,
    DateTime LocalStartTime,
    int? DistanceMeters,
    int? MovingSeconds,
    double? CaloriesKcal,
    int? ElevationGainMeters,
    double? AverageSpeedKmh,
    int? RiderEnergySharePercent,
    double? AverageRiderPowerWatts,
    FitBackupStatus FitStatus,
    string? FitProblem,
    string? FitPath)
{
    public static RideDto From(Ride ride) => new(
        ride.Id,
        ride.BikeId,
        ride.Title,
        ride.StartTime,
        TimeZones.ToRideLocal(ride.StartTime, ride.TimeZone),
        ride.DistanceMeters,
        ride.MovingSeconds,
        ride.CaloriesKcal,
        ride.ElevationGainMeters,
        ride.AverageSpeedKmh,
        ride.RiderEnergySharePercent,
        ride.AverageRiderPowerWatts,
        StatusOf(ride),
        ride.FitError,
        ride.FitPath);

    private static FitBackupStatus StatusOf(Ride ride) => ride switch
    {
        { FitPath: not null } => FitBackupStatus.Saved,
        { FitUnavailable: true } => FitBackupStatus.Unavailable,
        { FitError: not null } => FitBackupStatus.Failed,
        { EndTime: null } => FitBackupStatus.Processing,
        _ => FitBackupStatus.Pending
    };
}
