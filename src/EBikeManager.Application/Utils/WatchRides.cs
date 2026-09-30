using EBikeManager.Application.Models;

namespace EBikeManager.Application.Utils;

public static class WatchRides
{
    private const double MinimumOverlap = 0.5;

    public static List<HealthExercise> Overlapping(IEnumerable<HealthExercise> exercises, DateTime startUtc, DateTime endUtc) =>
        [.. exercises
            .Where(exercise => GoogleHealthJson.IsBikeExercise(exercise.ExerciseType) && !IsOurs(exercise) && Overlaps(exercise, startUtc, endUtc))
            .OrderByDescending(exercise => Overlap(exercise, startUtc, endUtc))];

    private static bool IsOurs(HealthExercise exercise) =>
        exercise.Name.Split('/')[^1].StartsWith(GoogleHealthEndpoints.DataPointPrefix, StringComparison.Ordinal);

    private static TimeSpan Overlap(HealthExercise exercise, DateTime startUtc, DateTime endUtc) =>
        (exercise.EndTime < endUtc ? exercise.EndTime : endUtc) - (exercise.StartTime > startUtc ? exercise.StartTime : startUtc);

    private static bool Overlaps(HealthExercise exercise, DateTime startUtc, DateTime endUtc)
    {
        var overlap = Overlap(exercise, startUtc, endUtc);
        if (overlap <= TimeSpan.Zero) return false;

        var shorter = Math.Min((exercise.EndTime - exercise.StartTime).TotalSeconds, (endUtc - startUtc).TotalSeconds);
        return overlap.TotalSeconds >= shorter * MinimumOverlap;
    }
}
