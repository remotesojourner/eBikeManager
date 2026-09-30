using EBikeManager.Application.Models;
using EBikeManager.Application.Utils;

namespace EBikeManager.UnitTests.Application.Utils;

public class WatchRidesTests
{
    private static readonly DateTime _start = new(2026, 9, 6, 16, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime _end = _start.AddHours(1);

    [Fact]
    public void ABikeRideYourWatchRecordedAtTheSameTimeIsFound()
    {
        HealthExercise[] exercises =
        [
            Exercise("watch-1", "OUTDOOR_BIKE", 5, 55),
            Exercise("watch-2", "BIKING", -30, 90),
            Exercise("watch-3", "WALKING", 0, 60),
            Exercise("ebike-ride-1", "ELECTRIC_BIKE", 0, 60),
            Exercise("watch-4", "BIKING", 50, 120),
            Exercise("watch-5", "BIKING", 60, 90)
        ];

        var found = WatchRides.Overlapping(exercises, _start, _end);

        Assert.Equal(["watch-2", "watch-1"], found.Select(exercise => exercise.Name.Split('/')[^1]));
    }

    private static HealthExercise Exercise(string id, string type, int startMinutes, int endMinutes) =>
        new($"users/1/dataTypes/exercise/dataPoints/{id}", type, null, _start.AddMinutes(startMinutes), _start.AddMinutes(endMinutes));
}
