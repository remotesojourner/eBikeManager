using System.Text.Json.Nodes;
using EBikeManager.Application.Models.Entities;
using EBikeManager.Application.Utils;

namespace EBikeManager.UnitTests.Application.Utils;

public class GoogleHealthExerciseTests
{
    private static readonly DateTime _start = new(2026, 9, 6, 16, 12, 0, DateTimeKind.Utc);

    [Fact]
    public void ARideBecomesAnElectricBikeWorkoutWithBoschsCalories()
    {
        var ride = new Ride
        {
            Id = "c4e78500-aa0d-11f1-bd38-a40dbc94029f",
            Title = "Edinburgh roundtrip",
            StartTime = _start,
            EndTime = _start.AddMinutes(57),
            TimeZone = "Europe/London",
            DistanceMeters = 3_064,
            MovingSeconds = 666,
            CaloriesKcal = 43,
            ElevationGainMeters = 12,
            AverageSpeedKmh = 16.6,
            FitHasGps = true,
            SummaryJson = """{"averageHeartRate":121.4}"""
        };

        var dataPoint = GoogleHealthJson.ExerciseDataPoint(GoogleHealthExercise.For(ride, "TENWAYS (Performance Line)"));

        Assert.Equal("users/me/dataTypes/exercise/dataPoints/ebike-c4e78500-aa0d-11f1-bd38-a40dbc94029f", (string?)dataPoint["name"]);
        var exercise = dataPoint["exercise"]!;
        Assert.Equal("ELECTRIC_BIKE", (string?)exercise["exerciseType"]);
        Assert.Equal("2026-09-06T16:12:00Z", (string?)exercise["interval"]!["startTime"]);
        Assert.Equal("2026-09-06T17:09:00Z", (string?)exercise["interval"]!["endTime"]);
        Assert.Equal("3600s", (string?)exercise["interval"]!["startUtcOffset"]);
        Assert.Equal("666s", (string?)exercise["activeDuration"]);
        Assert.True((bool?)exercise["exerciseMetadata"]!["hasGps"]);
        var metrics = exercise["metricsSummary"]!;
        Assert.Equal(43, (double?)metrics["caloriesKcal"]);
        Assert.Equal(3_064_000, (double?)metrics["distanceMillimeters"]);
        Assert.Equal(12_000, (double?)metrics["elevationGainMillimeters"]);
        Assert.Equal(4_611, (double?)metrics["averageSpeedMillimetersPerSecond"]);
        Assert.Equal("121", (string?)metrics["averageHeartRateBeatsPerMinute"]);
        Assert.Contains("Edinburgh roundtrip", (string?)exercise["notes"], StringComparison.Ordinal);
        Assert.Equal("TENWAYS (Performance Line)", (string?)dataPoint["dataSource"]!["device"]!["displayName"]);
    }

    [Fact]
    public void MissingFiguresAreLeftOut()
    {
        var ride = new Ride { Id = "ride-1", StartTime = _start, MovingSeconds = 600, TimeZone = "Not/AZone", SummaryJson = "{}" };

        var dataPoint = GoogleHealthJson.ExerciseDataPoint(GoogleHealthExercise.For(ride, null));

        var exercise = dataPoint["exercise"]!.AsObject();
        Assert.Empty(exercise["metricsSummary"]!.AsObject());
        Assert.Equal("0s", (string?)exercise["interval"]!["startUtcOffset"]);
        Assert.Equal("2026-09-06T16:22:00Z", (string?)exercise["interval"]!["endTime"]);
        Assert.False(dataPoint["dataSource"]!["device"]!.AsObject().ContainsKey("displayName"));
    }

    [Theory]
    [InlineData("c4e78500-aa0d-11f1-bd38-a40dbc94029f", "ebike-c4e78500-aa0d-11f1-bd38-a40dbc94029f")]
    [InlineData("Ride_42/A", "ebike-ride-42-a")]
    [InlineData("0123456789012345678901234567890123456789012345678901234567890123", "ebike-012345678901234567890123456789012345678901234567890123456")]
    public void DataPointIdsUseOnlyWhatGoogleAccepts(string rideId, string expected)
    {
        Assert.Equal(expected, GoogleHealthExercise.DataPointId(rideId));
    }

    [Fact]
    public void BatchDeletesListTheNames()
    {
        var body = GoogleHealthJson.BatchDelete(["a", "b"]);

        Assert.Equal("""{"names":["a","b"]}""", body.ToJsonString());
    }

    [Fact]
    public void ListedExercisesAreRead()
    {
        var page = JsonNode.Parse("""
            {
              "dataPoints": [
                { "name": "users/1/dataTypes/exercise/dataPoints/9", "exercise": { "exerciseType": "OUTDOOR_BIKE", "displayName": "Outdoor bike",
                  "interval": { "startTime": "2026-09-06T16:15:00Z", "endTime": "2026-09-06T17:05:30.5Z" } } },
                { "name": "users/1/dataTypes/exercise/dataPoints/10", "exercise": { "exerciseType": "WALKING" } }
              ],
              "nextPageToken": "next"
            }
            """)!;
        using var json = System.Text.Json.JsonDocument.Parse(page.ToJsonString());

        var exercise = Assert.Single(GoogleHealthJson.Exercises(json.RootElement));

        Assert.Equal(("OUTDOOR_BIKE", "Outdoor bike"), (exercise.ExerciseType, exercise.DisplayName));
        Assert.Equal(new DateTime(2026, 9, 6, 16, 15, 0, DateTimeKind.Utc), exercise.StartTime);
        Assert.Equal(DateTimeKind.Utc, exercise.EndTime.Kind);
    }
}
