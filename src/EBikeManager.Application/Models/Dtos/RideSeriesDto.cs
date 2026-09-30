namespace EBikeManager.Application.Models.Dtos;

public sealed record RideSeriesDto(
    IReadOnlyList<double> DistanceKm,
    IReadOnlyList<double?> Elevation,
    IReadOnlyList<double?> Speed,
    IReadOnlyList<double?> Cadence,
    IReadOnlyList<double?> Power,
    IReadOnlyList<double?> HeartRate,
    IReadOnlyList<double?> Latitude,
    IReadOnlyList<double?> Longitude)
{
    public static RideSeriesDto Empty { get; } = new([], [], [], [], [], [], [], []);

    public bool HasElevation => Elevation.Any(value => value != null);

    public bool HasSpeed => Speed.Any(value => value > 0);

    public bool HasCadence => Cadence.Any(value => value > 0);

    public bool HasPower => Power.Any(value => value > 0);

    public bool HasHeartRate => HeartRate.Any(value => value > 0);
}
