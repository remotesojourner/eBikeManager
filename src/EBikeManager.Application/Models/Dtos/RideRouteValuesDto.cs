namespace EBikeManager.Application.Models.Dtos;

public sealed record RideRouteValuesDto(
    IReadOnlyList<double?> Speed,
    IReadOnlyList<double?> Power,
    IReadOnlyList<double?> Cadence,
    IReadOnlyList<double?> Elevation,
    IReadOnlyList<double?> HeartRate)
{
    public static RideRouteValuesDto Empty { get; } = new([], [], [], [], []);
}
