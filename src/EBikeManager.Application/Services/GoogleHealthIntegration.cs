using System.Globalization;
using System.Net;
using EBikeManager.Application.Enums;
using EBikeManager.Application.Exceptions;
using EBikeManager.Application.Models;
using EBikeManager.Application.Models.Entities;
using EBikeManager.Application.Repositories.Interfaces;
using EBikeManager.Application.Resources;
using EBikeManager.Application.Services.Interfaces;
using EBikeManager.Application.Utils;
using Microsoft.Extensions.Logging;

namespace EBikeManager.Application.Services;

public sealed partial class GoogleHealthIntegration : IRideIntegration
{
    public const string IntegrationKey = "googleHealth";
    public const string Name = "Google Health";

    private static readonly TimeSpan _watchRideLookBack = TimeSpan.FromHours(2);

    private readonly IGoogleHealthApiService _api;
    private readonly GoogleHealthConnectionService _connection;
    private readonly ISettingsRepository _settings;
    private readonly ILogger<GoogleHealthIntegration> _logger;

    public GoogleHealthIntegration(IGoogleHealthApiService api, GoogleHealthConnectionService connection, ISettingsRepository settings, ILogger<GoogleHealthIntegration> logger)
    {
        _api = api;
        _connection = connection;
        _settings = settings;
        _logger = logger;
    }

    public string Key => IntegrationKey;

    public string DisplayName => Name;

    public async Task<ExportWindow?> GetExportWindowAsync(CancellationToken cancellationToken = default)
    {
        switch (await _connection.GetStatusAsync(cancellationToken))
        {
            case GoogleHealthConnectionStatus.NotConnected:
                return null;
            case GoogleHealthConnectionStatus.ReauthRequired:
                throw new IntegrationSignInRequiredException(ApplicationStrings.GoogleHealthSignInAgain);
        }

        var google = (await _settings.GetAsync(cancellationToken)).GoogleHealth;
        if (!google.HasUploadChoice) return null;
        return new ExportWindow(google.UploadsAllRides ? null : google.UploadFromUtc);
    }

    public async Task<RideExportOutcome> ExportAsync(Ride ride, string? bikeName, CancellationToken cancellationToken = default)
    {
        var upload = GoogleHealthExercise.For(ride, bikeName);
        var watchRides = await FindWatchRidesAsync(ride, upload, cancellationToken);
        if (watchRides is { Count: > 0 })
        {
            await RemoveEarlierUploadAsync(upload, cancellationToken);
            var described = string.Join(", ", watchRides.Select(exercise => Describe(exercise, ride.TimeZone)));
            return new RideExportOutcome(RideExportStatus.WatchRecorded, null, ApplicationStrings.Format(ApplicationStrings.GoogleHealthWatchRideBlocked, described));
        }

        var remoteId = await _api.CreateExerciseAsync(upload, cancellationToken);
        return new RideExportOutcome(RideExportStatus.Uploaded, remoteId, watchRides == null ? ApplicationStrings.GoogleHealthCannotReadWatchRides : null);
    }

    private async Task<IReadOnlyList<HealthExercise>?> FindWatchRidesAsync(Ride ride, ExerciseUpload upload, CancellationToken cancellationToken)
    {
        try
        {
            var exercises = await _api.ListWatchExercisesAsync(
                TimeZones.ToRideLocal(upload.StartTime - _watchRideLookBack, ride.TimeZone),
                TimeZones.ToRideLocal(upload.EndTime, ride.TimeZone),
                cancellationToken);
            var overlapping = WatchRides.Overlapping(exercises, upload.StartTime, upload.EndTime);
            var found = exercises.Count == 0 ? "none" : string.Join(", ", exercises.Select(exercise => $"{exercise.ExerciseType} {Describe(exercise, ride.TimeZone)}"));
            LogWatchRidesChecked(ride.Id, found, overlapping.Count);
            return overlapping;
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.Forbidden)
        {
            return null;
        }
    }

    private async Task RemoveEarlierUploadAsync(ExerciseUpload upload, CancellationToken cancellationToken)
    {
        try
        {
            await _api.RemoveOwnExerciseAsync(upload.DataPointId, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            LogEarlierUploadKept(ex, upload.DataPointId);
        }
    }

    private static string Describe(HealthExercise exercise, string? timeZone) => string.Create(
        CultureInfo.InvariantCulture,
        $"{exercise.DisplayName ?? exercise.ExerciseType} {TimeZones.ToRideLocal(exercise.StartTime, timeZone):HH:mm}–{TimeZones.ToRideLocal(exercise.EndTime, timeZone):HH:mm}");

    [LoggerMessage(Level = LogLevel.Information, Message = "Google Health workouts from wearables around ride {RideId}: {Found}; {Overlapping} overlap the ride")]
    private partial void LogWatchRidesChecked(string rideId, string found, int overlapping);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Could not remove eBike Manager's earlier upload {DataPointId} from Google Health")]
    private partial void LogEarlierUploadKept(Exception exception, string dataPointId);
}
