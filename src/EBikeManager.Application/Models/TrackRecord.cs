namespace EBikeManager.Application.Models;

public sealed record TrackRecord(
    DateTime Time,
    double? Latitude = null,
    double? Longitude = null,
    double? AltitudeMeters = null,
    double? SpeedMetresPerSecond = null,
    double? Cadence = null,
    double? PowerWatts = null,
    double? HeartRate = null,
    double? DistanceMeters = null);
