namespace EBikeManager.Application.Models.Dtos;

public sealed record RideTrackDto(IReadOnlyList<double[]> Route, RideRouteValuesDto RouteValues, RideSeriesDto Series, IReadOnlyList<RideSplitDto> Splits)
{
    public bool HasRoute => Route.Count > 1;

    public bool HasCharts => Series.DistanceKm.Count > 1;
}
