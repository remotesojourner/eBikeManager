using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using EBikeManager.Application.Extensions;
using EBikeManager.Application.Models;

namespace EBikeManager.Application.Utils;

public static class GoogleHealthJson
{
    public const string ElectricBike = "ELECTRIC_BIKE";

    private const string ExerciseName = "Electric bike";
    private const string Manufacturer = "Bosch";
    private const string InstantFormat = "yyyy-MM-dd'T'HH:mm:ss'Z'";

    private static readonly string[] _bikeTypes = ["BIKING", "OUTDOOR_BIKE", "MOUNTAIN_BIKE", ElectricBike];

    public static string DataPointName(string dataPointId) => $"{GoogleHealthEndpoints.ExercisePoints}/{dataPointId}";

    public static JsonObject ExerciseDataPoint(ExerciseUpload upload)
    {
        var metrics = new JsonObject();
        if (upload.CaloriesKcal is { } kcal) metrics["caloriesKcal"] = Math.Round(kcal, 1);
        if (upload.DistanceMeters is { } metres) metrics["distanceMillimeters"] = Math.Round(metres * 1000);
        if (upload.ElevationGainMeters is { } climb) metrics["elevationGainMillimeters"] = Math.Round(climb * 1000);
        if (upload.AverageSpeedKmh is { } speed) metrics["averageSpeedMillimetersPerSecond"] = Math.Round(speed / 3.6 * 1000);
        if (upload.AverageHeartRate is { } heartRate) metrics["averageHeartRateBeatsPerMinute"] = Math.Round(heartRate).ToString(CultureInfo.InvariantCulture);

        var exercise = new JsonObject
        {
            ["exerciseType"] = ElectricBike,
            ["displayName"] = ExerciseName,
            ["notes"] = upload.Notes,
            ["interval"] = new JsonObject
            {
                ["startTime"] = Instant(upload.StartTime),
                ["endTime"] = Instant(upload.EndTime),
                ["startUtcOffset"] = Duration(upload.StartOffset),
                ["endUtcOffset"] = Duration(upload.EndOffset)
            },
            ["exerciseMetadata"] = new JsonObject { ["hasGps"] = upload.HasGps },
            ["metricsSummary"] = metrics
        };
        if (upload.ActiveSeconds is { } active) exercise["activeDuration"] = Duration(TimeSpan.FromSeconds(Math.Round(active)));

        var device = new JsonObject { ["manufacturer"] = Manufacturer };
        if (upload.DeviceName != null) device["displayName"] = upload.DeviceName;

        return new JsonObject
        {
            ["name"] = DataPointName(upload.DataPointId),
            ["dataSource"] = new JsonObject { ["recordingMethod"] = "ACTIVELY_MEASURED", ["device"] = device },
            ["exercise"] = exercise
        };
    }

    public static JsonObject BatchDelete(IEnumerable<string> names) =>
        new() { ["names"] = new JsonArray([.. names.Select(name => (JsonNode?)JsonValue.Create(name))]) };

    public static string Instant(DateTime utc) => TimeZones.AsUtc(utc).ToString(InstantFormat, CultureInfo.InvariantCulture);

    public static string Duration(TimeSpan span) => string.Create(CultureInfo.InvariantCulture, $"{(long)Math.Round(span.TotalSeconds)}s");

    public static string CivilTime(DateTime local) => local.ToString("yyyy-MM-dd'T'HH:mm:ss", CultureInfo.InvariantCulture);

    public static List<HealthExercise> Exercises(JsonElement page) => [.. page.Items("dataPoints").Select(Exercise).OfType<HealthExercise>()];

    public static GoogleHealthError? OperationError(JsonElement operation) =>
        operation.Property("error") is { ValueKind: JsonValueKind.Object } error ? new GoogleHealthError(error.Whole("code") ?? 0, error.Text("message") ?? "") : null;

    public static string? CreatedName(JsonElement operation) => operation.Property("response").Text("name");

    public static bool IsBikeExercise(string? exerciseType) => exerciseType != null && _bikeTypes.Contains(exerciseType);

    private static HealthExercise? Exercise(JsonElement point)
    {
        var exercise = point.Property("exercise");
        var interval = exercise.Property("interval");
        if (point.Text("name") is not { } name || ReadInstant(interval.Text("startTime")) is not { } start || ReadInstant(interval.Text("endTime")) is not { } end) return null;

        return new HealthExercise(name, exercise.Text("exerciseType"), exercise.Text("displayName"), start, end);
    }

    private static DateTime? ReadInstant(string? text) =>
        DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var instant) ? instant : null;
}
