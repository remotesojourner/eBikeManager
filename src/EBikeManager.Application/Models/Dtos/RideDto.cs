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
    BackupStatus FitStatus,
    string? FitProblem,
    string? FitPath,
    BackupStatus GpxStatus,
    string? GpxPath)
{
    public bool Finished => FitStatus != BackupStatus.Processing;

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
        FitStatusOf(ride),
        ride.FitError,
        ride.FitPath,
        GpxStatusOf(ride),
        ride.GpxPath);

    private static BackupStatus FitStatusOf(Ride ride) => ride switch
    {
        { FitPath: not null } => BackupStatus.Saved,
        { FitUnavailable: true } => BackupStatus.Unavailable,
        { FitError: not null } => BackupStatus.Failed,
        { EndTime: null } => BackupStatus.Processing,
        _ => BackupStatus.Pending
    };

    private static BackupStatus GpxStatusOf(Ride ride) => ride switch
    {
        { GpxPath: not null } => BackupStatus.Saved,
        { GpxUnavailable: true } => BackupStatus.Unavailable,
        { EndTime: null } => BackupStatus.Processing,
        _ => BackupStatus.Pending
    };
}
