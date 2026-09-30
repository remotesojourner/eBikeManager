namespace EBikeManager.Application.Models.Dtos;

public sealed record RideTotalsDto(int Rides, double DistanceMeters, double MovingSeconds, double CaloriesKcal, double ElevationGainMeters)
{
    public static RideTotalsDto None { get; } = new(0, 0, 0, 0, 0);
}
