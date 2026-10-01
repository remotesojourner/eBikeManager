using System.Text.Json;
using System.Text.RegularExpressions;
using EBikeManager.Application.Extensions;
using EBikeManager.Application.Models;
using EBikeManager.Application.Models.Entities;
using EBikeManager.Application.Resources;

namespace EBikeManager.Application.Utils;

public static partial class GoogleHealthExercise
{
    private const int MaxIdLength = 63;

    public static string DataPointId(string rideId)
    {
        var id = GoogleHealthEndpoints.DataPointPrefix + InvalidIdCharacters().Replace(rideId.ToLowerInvariant(), "-");
        return id.Length > MaxIdLength ? id[..MaxIdLength] : id;
    }

    public static ExerciseUpload For(Ride ride)
    {
        var bike = ride.BikeModel ?? ride.BikeName;
        var end = ride.EndTime ?? ride.StartTime.AddSeconds(ride.MovingSeconds ?? 0);
        return new ExerciseUpload(
            DataPointId(ride.Id),
            ride.StartTime,
            end,
            Offset(ride.TimeZone, ride.StartTime),
            Offset(ride.TimeZone, end),
            ride.MovingSeconds,
            ride.CaloriesKcal,
            ride.DistanceMeters,
            ride.ElevationGainMeters,
            ride.AverageSpeedKmh,
            AverageHeartRate(ride.SummaryJson),
            ride.FitHasGps == true,
            ApplicationStrings.Format(ApplicationStrings.GoogleHealthExerciseNotes, ride.Title ?? ApplicationStrings.GoogleHealthUntitledRide, bike ?? ApplicationStrings.GoogleHealthUnnamedBike),
            bike);
    }

    private static TimeSpan Offset(string? timeZone, DateTime utc) =>
        timeZone != null && TimeZones.TryFindTimeZone(timeZone, out var zone) ? zone.GetUtcOffset(TimeZones.AsUtc(utc)) : TimeSpan.Zero;

    private static double? AverageHeartRate(string summaryJson)
    {
        try
        {
            using var summary = JsonDocument.Parse(summaryJson);
            return summary.RootElement.Number("averageHeartRate");
        }
        catch (JsonException)
        {
            return null;
        }
    }

    [GeneratedRegex("[^a-z0-9-]")]
    private static partial Regex InvalidIdCharacters();
}
