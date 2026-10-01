namespace EBikeManager.Application.Models.Dtos;

public sealed record StatisticsDto(int Bikes, int Rides, double MileageKm, long MovingTimeSeconds, double ElevationGainMeters, double CaloriesKcal)
{
    public static StatisticsDto From(int bikes, RideTotalsDto totals) => new(
        bikes,
        totals.Rides,
        Math.Round(totals.DistanceMeters / 1000, 1),
        (long)Math.Round(totals.MovingSeconds),
        Math.Round(totals.ElevationGainMeters),
        Math.Round(totals.CaloriesKcal));
}
