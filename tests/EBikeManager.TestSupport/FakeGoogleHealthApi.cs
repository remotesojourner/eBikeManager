using System.Net;
using EBikeManager.Application.Exceptions;
using EBikeManager.Application.Models;
using EBikeManager.Application.Services.Interfaces;
using EBikeManager.Application.Utils;

namespace EBikeManager.TestSupport;

internal sealed class FakeGoogleHealthApi : IGoogleHealthApiService
{
    public List<ExerciseUpload> Uploads { get; } = [];

    public List<HealthExercise> WatchExercises { get; } = [];

    public List<string> Removed { get; } = [];

    public HashSet<string> FailingDataPoints { get; } = [];

    public bool CanReadWatchRides { get; set; } = true;

    public bool SignInExpired { get; set; }

    public Task<string> CreateExerciseAsync(ExerciseUpload upload, CancellationToken cancellationToken = default)
    {
        if (SignInExpired) return Task.FromException<string>(new IntegrationSignInRequiredException("Google no longer accepts the sign-in."));
        if (FailingDataPoints.Contains(upload.DataPointId)) return Task.FromException<string>(new HttpRequestException("Google Health answered 503 Service Unavailable: try later"));

        lock (Uploads) Uploads.Add(upload);
        return Task.FromResult($"users/1234/dataTypes/exercise/dataPoints/{upload.DataPointId}");
    }

    public Task<IReadOnlyList<HealthExercise>> ListWatchExercisesAsync(DateTime civilFrom, DateTime civilTo, CancellationToken cancellationToken = default) =>
        CanReadWatchRides
            ? Task.FromResult<IReadOnlyList<HealthExercise>>([.. WatchExercises])
            : Task.FromException<IReadOnlyList<HealthExercise>>(new HttpRequestException("Google Health answered 403 Forbidden", null, HttpStatusCode.Forbidden));

    public Task RemoveOwnExerciseAsync(string dataPointId, CancellationToken cancellationToken = default)
    {
        lock (Removed) Removed.Add(dataPointId);
        return Task.CompletedTask;
    }

    public HealthExercise AddWatchRide(DateTime start, DateTime end, string type = "OUTDOOR_BIKE")
    {
        var exercise = new HealthExercise($"users/1234/dataTypes/exercise/dataPoints/watch-{WatchExercises.Count + 1}", type, "Outdoor bike", start, end);
        WatchExercises.Add(exercise);
        return exercise;
    }

    public static string DataPointFor(string rideId) => GoogleHealthExercise.DataPointId(rideId);
}
