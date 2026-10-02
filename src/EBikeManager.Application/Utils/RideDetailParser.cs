using System.Text.Json;
using EBikeManager.Application.Extensions;
using EBikeManager.Application.Models.Dtos;
using EBikeManager.Application.Models.Entities;

namespace EBikeManager.Application.Utils;

public static class RideDetailParser
{
    public static RideDetailDto Parse(Ride ride, RideTrackDto? track, string? trackProblem)
    {
        using var document = Read(ride.SummaryJson);
        var summary = document?.RootElement ?? default;

        return new RideDetailDto(
            ride.Id,
            ride.Title,
            ride.BikeName,
            ride.BikeModel,
            TimeZones.ToRideLocal(ride.StartTime, ride.TimeZone),
            ride.DistanceMeters ?? summary.Number("distance"),
            ride.MovingSeconds ?? summary.Number("durationWithoutStops"),
            ride.EndTime is { } end ? (end - ride.StartTime).TotalSeconds : null,
            ride.AverageSpeedKmh ?? summary.Number("averageSpeed"),
            summary.Number("maximumSpeed"),
            summary.Number("averageCadence"),
            summary.Number("maximumCadence"),
            ride.AverageRiderPowerWatts ?? summary.Number("averageRiderPower"),
            summary.Number("maximumRiderPower"),
            summary.Number("averageHeartRate"),
            summary.Number("maximumHeartRate"),
            ride.ElevationGainMeters ?? summary.Number("elevationGain"),
            summary.Number("elevationLoss"),
            ride.CaloriesKcal ?? summary.Number("caloriesBurnt"),
            ride.RiderEnergySharePercent ?? summary.Number("riderEnergyShare"),
            summary.Number("co2EmissionsGrams"),
            summary.Number("co2EmissionsCarEquivalentGrams"),
            summary.Property("brakeEvents").Whole("amountOfAbsInterventionEvents"),
            AssistModes(summary),
            ride.FitPath != null,
            ride.GpxPath != null,
            track,
            trackProblem);
    }

    private static List<AssistModeShareDto> AssistModes(JsonElement summary)
    {
        var used = summary.Items("assistModeUsage")
            .Select(usage => (Name: usage.Text("name") ?? usage.Text("assistModeConfigId"), Color: BoschJson.Colour(usage.Number("color")), Meters: usage.Number("assistModeUsage") ?? 0))
            .Where(usage => usage.Name != null && usage.Meters > 0)
            .ToList();
        var total = used.Sum(usage => usage.Meters);

        return [.. used
            .OrderByDescending(usage => usage.Meters)
            .Select(usage => new AssistModeShareDto(usage.Name!, usage.Color, usage.Meters, Math.Round(usage.Meters / total * 100, 1)))];
    }

    private static JsonDocument? Read(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;

        try
        {
            return JsonDocument.Parse(json);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
