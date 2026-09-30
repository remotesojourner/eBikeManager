using System.Net;
using System.Web;
using EBikeManager.Application.Models;
using EBikeManager.Application.Services;
using EBikeManager.TestSupport;
using Microsoft.Extensions.Logging.Abstractions;

namespace EBikeManager.UnitTests.Application.Services;

public class GoogleHealthApiServiceTests
{
    private const string Created = "users/77/dataTypes/exercise/dataPoints/ebike-ride-1";

    private static readonly DateTime _start = new(2026, 9, 6, 16, 0, 0, DateTimeKind.Utc);
    private static readonly ExerciseUpload _upload = new(
        "ebike-ride-1", _start, _start.AddHours(1), TimeSpan.FromHours(1), TimeSpan.FromHours(1), 3_000, 410, 20_000, 120, 21.8, null, true, "notes", "TENWAYS");

    [Fact]
    public async Task AnExerciseIsCreatedWithItsOwnId()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var handler = new RecordingHandler(request => request.Method == HttpMethod.Get
            ? RecordingHandler.Json("{}")
            : RecordingHandler.Json($$$"""{"done":true,"response":{"name":"{{{Created}}}"}}"""));

        var name = await Service(handler).CreateExerciseAsync(_upload, cancellationToken);

        Assert.Equal(Created, name);
        Assert.Equal(2, handler.Requests.Count);
        Assert.Equal($"https://health.googleapis.com/v4/{Created}", handler.Requests[1].Uri!.ToString());
        var request = handler.Requests[0];
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("https://health.googleapis.com/v4/users/me/dataTypes/exercise/dataPoints", request.Uri!.ToString());
        Assert.Contains("\"name\":\"users/me/dataTypes/exercise/dataPoints/ebike-ride-1\"", request.Body, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(HttpStatusCode.Conflict, """{"error":{"code":409,"message":"exists"}}""")]
    [InlineData(HttpStatusCode.OK, """{"done":true,"error":{"code":6,"message":"ALREADY_EXISTS"}}""")]
    public async Task AnExerciseThatIsAlreadyThereCountsAsUploaded(HttpStatusCode status, string body)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var handler = new RecordingHandler(request => request.Method == HttpMethod.Get ? RecordingHandler.Json("{}") : RecordingHandler.Json(body, status));

        Assert.Equal("users/me/dataTypes/exercise/dataPoints/ebike-ride-1", await Service(handler).CreateExerciseAsync(_upload, cancellationToken));
    }

    [Fact]
    public async Task ARideGoogleTurnedDownIsNotCountedAsUploaded()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var handler = new RecordingHandler(request => request.Method == HttpMethod.Get
            ? RecordingHandler.Json("""{"error":{"code":404,"message":"Not found"}}""", HttpStatusCode.NotFound)
            : RecordingHandler.Json("""{"error":{"code":409,"message":"An exercise already exists in this time range"}}""", HttpStatusCode.Conflict));

        var error = await Assert.ThrowsAsync<HttpRequestException>(() => Service(handler).CreateExerciseAsync(_upload, cancellationToken));

        Assert.Equal("Google Health didn't keep this ride: An exercise already exists in this time range", error.Message);
    }

    [Fact]
    public async Task ARideThatIsStillMissingAfterUploadingIsNotCountedAsUploaded()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var handler = new RecordingHandler(request => request.Method == HttpMethod.Get
            ? new HttpResponseMessage(HttpStatusCode.NotFound)
            : RecordingHandler.Json($$$"""{"done":true,"response":{"name":"{{{Created}}}"}}"""));

        var error = await Assert.ThrowsAsync<HttpRequestException>(() => Service(handler).CreateExerciseAsync(_upload, cancellationToken));

        Assert.Contains("wasn't there when eBike Manager checked", error.Message, StringComparison.Ordinal);
        Assert.Equal(3, handler.Requests.Count(request => request.Method == HttpMethod.Get));
    }

    [Fact]
    public async Task FailuresCarryGooglesMessage()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var handler = new RecordingHandler(_ => RecordingHandler.Json("""{"error":{"code":400,"message":"Invalid exercise interval"}}""", HttpStatusCode.BadRequest));

        var error = await Assert.ThrowsAsync<HttpRequestException>(() => Service(handler).CreateExerciseAsync(_upload, cancellationToken));

        Assert.Contains("Invalid exercise interval", error.Message, StringComparison.Ordinal);
        Assert.Equal(HttpStatusCode.BadRequest, error.StatusCode);
    }

    [Fact]
    public async Task WatchRidesAreListedPageByPageFromWearablesOnly()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var handler = new RecordingHandler(request => RecordingHandler.Json(request.Uri!.Query.Contains("pageToken", StringComparison.Ordinal)
            ? """{"dataPoints":[{"name":"n2","exercise":{"exerciseType":"BIKING","interval":{"startTime":"2026-09-06T17:00:00Z","endTime":"2026-09-06T18:00:00Z"}}}]}"""
            : """{"dataPoints":[{"name":"n1","exercise":{"exerciseType":"BIKING","interval":{"startTime":"2026-09-06T15:00:00Z","endTime":"2026-09-06T16:00:00Z"}}}],"nextPageToken":"p2"}"""));

        var exercises = await Service(handler).ListWatchExercisesAsync(new DateTime(2026, 9, 6, 15, 0, 0), new DateTime(2026, 9, 6, 18, 0, 0), cancellationToken);

        Assert.Equal(["n1", "n2"], exercises.Select(exercise => exercise.Name));
        var query = HttpUtility.ParseQueryString(handler.Requests[0].Uri!.Query);
        Assert.Equal("users/me/dataSourceFamilies/google-wearables", query["dataSourceFamily"]);
        Assert.Equal("25", query["pageSize"]);
        Assert.Equal("exercise.interval.civil_start_time >= \"2026-09-06T15:00:00\" AND exercise.interval.civil_start_time < \"2026-09-06T18:00:00\"", query["filter"]);
        Assert.Equal("p2", HttpUtility.ParseQueryString(handler.Requests[1].Uri!.Query)["pageToken"]);
    }

    [Fact]
    public async Task OnlyAnEarlierUploadThatExistsIsRemoved()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var missing = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound));
        var existing = new RecordingHandler(request => RecordingHandler.Json(request.Method == HttpMethod.Get ? "{}" : """{"done":true}"""));

        await Service(missing).RemoveOwnExerciseAsync("ebike-ride-1", cancellationToken);
        await Service(existing).RemoveOwnExerciseAsync("ebike-ride-1", cancellationToken);

        Assert.Single(missing.Requests);
        var delete = Assert.Single(existing.Requests, request => request.Method == HttpMethod.Post);
        Assert.EndsWith("/dataPoints:batchDelete", delete.Uri!.AbsolutePath, StringComparison.Ordinal);
        Assert.Equal("""{"names":["users/me/dataTypes/exercise/dataPoints/ebike-ride-1"]}""", delete.Body);
    }

    private static GoogleHealthApiService Service(RecordingHandler handler) =>
        new(new HttpClient(handler), NullLogger<GoogleHealthApiService>.Instance) { ConfirmationDelay = TimeSpan.Zero };
}
