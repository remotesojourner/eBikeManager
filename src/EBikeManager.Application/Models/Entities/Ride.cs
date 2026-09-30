namespace EBikeManager.Application.Models.Entities;

public class Ride
{
    public string Id { get; set; } = string.Empty;

    public string BikeId { get; set; } = string.Empty;

    public string? Title { get; set; }

    public DateTime StartTime { get; set; }

    public DateTime? EndTime { get; set; }

    public string? TimeZone { get; set; }

    public int? DistanceMeters { get; set; }

    public int? MovingSeconds { get; set; }

    public double? CaloriesKcal { get; set; }

    public int? ElevationGainMeters { get; set; }

    public double? AverageSpeedKmh { get; set; }

    public int? RiderEnergySharePercent { get; set; }

    public double? AverageRiderPowerWatts { get; set; }

    public string SummaryJson { get; set; } = "{}";

    public DateTime FirstSeenAt { get; set; }

    public string? FitPath { get; set; }

    public string? FitSha256 { get; set; }

    public long? FitSizeBytes { get; set; }

    public DateTime? FitDownloadedAt { get; set; }

    public bool FitUnavailable { get; set; }

    public string? FitError { get; set; }

    public double? FitTimerSeconds { get; set; }

    public double? FitDistanceMeters { get; set; }

    public int? FitAveragePowerWatts { get; set; }

    public bool? FitHasGps { get; set; }
}
