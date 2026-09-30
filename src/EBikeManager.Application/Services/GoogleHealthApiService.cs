using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EBikeManager.Application.Extensions;
using EBikeManager.Application.Models;
using EBikeManager.Application.Resources;
using EBikeManager.Application.Services.Interfaces;
using EBikeManager.Application.Utils;
using Microsoft.Extensions.Logging;

namespace EBikeManager.Application.Services;

internal sealed partial class GoogleHealthApiService : IGoogleHealthApiService
{
    private const int PageSize = 25;
    private const int MaxPages = 8;
    private const int ConfirmationAttempts = 3;

    private readonly HttpClient _http;
    private readonly ILogger<GoogleHealthApiService> _logger;

    public GoogleHealthApiService(HttpClient http, ILogger<GoogleHealthApiService> logger)
    {
        _http = http;
        _logger = logger;
    }

    internal TimeSpan ConfirmationDelay { get; set; } = TimeSpan.FromSeconds(1);

    public async Task<string> CreateExerciseAsync(ExerciseUpload upload, CancellationToken cancellationToken = default)
    {
        var requested = GoogleHealthJson.DataPointName(upload.DataPointId);
        using var content = JsonContent.Create(GoogleHealthJson.ExerciseDataPoint(upload));
        using var response = await _http.PostAsync(new Uri(GoogleHealthEndpoints.Api, GoogleHealthEndpoints.ExercisePoints), content, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        var answer = Shorten(body);
        LogCreateAnswered(upload.DataPointId, (int)response.StatusCode, answer);

        var (name, googleSaid) = CreatedOrExisting(response, body, requested);
        if (await ConfirmAsync(name, cancellationToken)) return name;

        throw new HttpRequestException(ApplicationStrings.Format(ApplicationStrings.GoogleHealthNotKept, googleSaid ?? ApplicationStrings.GoogleHealthMissingAfterUpload));
    }

    public async Task<IReadOnlyList<HealthExercise>> ListWatchExercisesAsync(DateTime civilFrom, DateTime civilTo, CancellationToken cancellationToken = default)
    {
        var filter = $"exercise.interval.civil_start_time >= \"{GoogleHealthJson.CivilTime(civilFrom)}\" AND exercise.interval.civil_start_time < \"{GoogleHealthJson.CivilTime(civilTo)}\"";
        var exercises = new List<HealthExercise>();
        string? pageToken = null;
        for (var page = 0; page < MaxPages; page++)
        {
            var query = $"{GoogleHealthEndpoints.ExercisePoints}?pageSize={PageSize}&dataSourceFamily={Uri.EscapeDataString(GoogleHealthEndpoints.WearablesFamily)}&filter={Uri.EscapeDataString(filter)}";
            if (pageToken != null) query += $"&pageToken={Uri.EscapeDataString(pageToken)}";

            using var response = await _http.GetAsync(new Uri(GoogleHealthEndpoints.Api, query), cancellationToken);
            await EnsureSuccessAsync(response, cancellationToken);
            using var json = await ReadAsync(response, cancellationToken);
            exercises.AddRange(GoogleHealthJson.Exercises(json.RootElement));

            pageToken = json.RootElement.Text("nextPageToken");
            if (string.IsNullOrEmpty(pageToken)) break;
        }

        return exercises;
    }

    public async Task RemoveOwnExerciseAsync(string dataPointId, CancellationToken cancellationToken = default)
    {
        var name = GoogleHealthJson.DataPointName(dataPointId);
        using (var existing = await _http.GetAsync(new Uri(GoogleHealthEndpoints.Api, name), cancellationToken))
        {
            if (existing.StatusCode == HttpStatusCode.NotFound) return;
            await EnsureSuccessAsync(existing, cancellationToken);
        }

        using var content = JsonContent.Create(GoogleHealthJson.BatchDelete([name]));
        using var response = await _http.PostAsync(new Uri(GoogleHealthEndpoints.Api, GoogleHealthEndpoints.ExercisePoints + ":batchDelete"), content, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        using var operation = await ReadAsync(response, cancellationToken);
        if (GoogleHealthJson.OperationError(operation.RootElement) is { Code: not GoogleHealthError.NotFound } error) throw Failed(error);
    }

    private static (string Name, string? GoogleSaid) CreatedOrExisting(HttpResponseMessage response, string body, string requested)
    {
        if (response.StatusCode == HttpStatusCode.Conflict) return (requested, MessageFrom(body));
        if (!response.IsSuccessStatusCode) throw RequestFailed(response, body);

        try
        {
            using var operation = JsonDocument.Parse(body);
            return GoogleHealthJson.OperationError(operation.RootElement) switch
            {
                null => (GoogleHealthJson.CreatedName(operation.RootElement) ?? requested, null),
                { Code: GoogleHealthError.AlreadyExists } error => (requested, error.Message),
                var error => throw Failed(error)
            };
        }
        catch (JsonException)
        {
            return (requested, null);
        }
    }

    private async Task<bool> ConfirmAsync(string name, CancellationToken cancellationToken)
    {
        for (var attempt = 1; ; attempt++)
        {
            using var response = await _http.GetAsync(new Uri(GoogleHealthEndpoints.Api, name), cancellationToken);
            if (response.IsSuccessStatusCode) return true;
            if (response.StatusCode != HttpStatusCode.NotFound) throw RequestFailed(response, await response.Content.ReadAsStringAsync(cancellationToken));

            LogNotFoundYet(name, attempt);
            if (attempt >= ConfirmationAttempts) return false;
            await Task.Delay(ConfirmationDelay, cancellationToken);
        }
    }

    private static async Task<JsonDocument> ReadAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        return await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
    }

    private static HttpRequestException Failed(GoogleHealthError error) =>
        new(ApplicationStrings.Format(ApplicationStrings.GoogleHealthOperationFailed, error.Code, error.Message));

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode) return;

        throw RequestFailed(response, await response.Content.ReadAsStringAsync(cancellationToken));
    }

    private static HttpRequestException RequestFailed(HttpResponseMessage response, string body) =>
        new(ApplicationStrings.Format(ApplicationStrings.GoogleHealthRequestFailed, (int)response.StatusCode, response.ReasonPhrase, MessageFrom(body)), null, response.StatusCode);

    private static string MessageFrom(string body)
    {
        try
        {
            using var json = JsonDocument.Parse(body);
            return json.RootElement.Property("error").Text("message") ?? Shorten(body);
        }
        catch (JsonException)
        {
            return Shorten(body);
        }
    }

    private static string Shorten(string text) => text.Length > 300 ? text[..300] + "…" : text;

    [LoggerMessage(Level = LogLevel.Information, Message = "Google Health answered {Status} to uploading {DataPointId}: {Body}")]
    private partial void LogCreateAnswered(string dataPointId, int status, string body);

    [LoggerMessage(Level = LogLevel.Information, Message = "Google Health doesn't have {Name} yet (check {Attempt})")]
    private partial void LogNotFoundYet(string name, int attempt);
}
