using EBikeManager.Application.Enums;
using EBikeManager.Application.Exceptions;
using EBikeManager.Application.Models;
using EBikeManager.Application.Models.Entities;
using EBikeManager.Application.Repositories.Interfaces;
using EBikeManager.Application.Resources;
using EBikeManager.Application.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace EBikeManager.Application.Services;

public sealed partial class IntegrationSyncService
{
    public const int MaxRidesPerRun = 100;

    private const int MaxFailuresInARow = 3;

    private readonly IEnumerable<IRideIntegration> _integrations;
    private readonly IRideExportRepository _exports;
    private readonly IRideRepository _rides;
    private readonly IBikeRepository _bikes;
    private readonly SyncStateService _state;
    private readonly TimeProvider _time;
    private readonly ILogger<IntegrationSyncService> _logger;

    public IntegrationSyncService(
        IEnumerable<IRideIntegration> integrations,
        IRideExportRepository exports,
        IRideRepository rides,
        IBikeRepository bikes,
        SyncStateService state,
        TimeProvider time,
        ILogger<IntegrationSyncService> logger)
    {
        _integrations = integrations;
        _exports = exports;
        _rides = rides;
        _bikes = bikes;
        _state = state;
        _time = time;
        _logger = logger;
    }

    public async Task<IntegrationRunResult> RunAsync(CancellationToken cancellationToken = default)
    {
        var uploaded = 0;
        var problems = new List<string>();
        foreach (var integration in _integrations)
        {
            try
            {
                uploaded += await ExportAsync(integration, problems, cancellationToken);
            }
            catch (IntegrationSignInRequiredException ex)
            {
                LogSignInRequired(integration.Key, ex.Message);
                problems.Add(ApplicationStrings.Format(ApplicationStrings.IntegrationProblem, integration.DisplayName, ex.Message));
            }
        }

        return new IntegrationRunResult(uploaded, problems);
    }

    private async Task<int> ExportAsync(IRideIntegration integration, List<string> problems, CancellationToken cancellationToken)
    {
        if (await integration.GetExportWindowAsync(cancellationToken) is not { } window) return 0;

        var rides = await _exports.GetRidesToExportAsync(integration.Key, window.FromUtc, MaxRidesPerRun, cancellationToken);
        if (rides.Count == 0) return 0;

        var bikeNames = await BikeNamesAsync(cancellationToken);
        var uploaded = 0;
        var failed = 0;
        var watchRecorded = 0;
        var failuresInARow = 0;
        foreach (var (ride, index) in rides.Select((ride, index) => (ride, index)))
        {
            _state.ReportProgress(ApplicationStrings.Format(ApplicationStrings.SyncProgressUploading, integration.DisplayName, index + 1, rides.Count));
            switch ((await ExportRideAsync(integration, ride, bikeNames.GetValueOrDefault(ride.BikeId), cancellationToken)).Status)
            {
                case RideExportStatus.Uploaded:
                    uploaded++;
                    failuresInARow = 0;
                    break;
                case RideExportStatus.WatchRecorded:
                    watchRecorded++;
                    failuresInARow = 0;
                    break;
                default:
                    failed++;
                    failuresInARow++;
                    break;
            }

            if (failuresInARow >= MaxFailuresInARow) break;
        }

        if (failed > 0) problems.Add(ApplicationStrings.Format(ApplicationStrings.IntegrationUploadsFailed, integration.DisplayName, failed));
        if (watchRecorded > 0) problems.Add(ApplicationStrings.Format(ApplicationStrings.IntegrationWatchRecordedRides, integration.DisplayName, watchRecorded));
        return uploaded;
    }

    public async Task<OperationResult> ExportOneAsync(string integrationKey, string rideId, CancellationToken cancellationToken = default)
    {
        if (_integrations.FirstOrDefault(integration => integration.Key == integrationKey) is not { } integration) return OperationResult.NotFound(ApplicationStrings.IntegrationUnknown);
        if (_state.Status.IsRunning) return OperationResult.Conflict(ApplicationStrings.IntegrationUploadWhileSyncing);
        if (await _rides.FindAsync(rideId, cancellationToken) is not { } ride) return OperationResult.NotFound(ApplicationStrings.RideNotFound);
        if (ride.EndTime == null) return OperationResult.Invalid(ApplicationStrings.IntegrationRideNotFinished);

        try
        {
            if (await integration.GetExportWindowAsync(cancellationToken) == null)
                return OperationResult.Invalid(ApplicationStrings.Format(ApplicationStrings.IntegrationNotConnected, integration.DisplayName));

            var export = await ExportRideAsync(integration, ride, (await BikeNamesAsync(cancellationToken)).GetValueOrDefault(ride.BikeId), cancellationToken);
            return export.Status == RideExportStatus.Uploaded ? OperationResult.Ok() : OperationResult.Invalid(export.Problem ?? ApplicationStrings.IntegrationUploadFailed);
        }
        catch (IntegrationSignInRequiredException ex)
        {
            LogSignInRequired(integration.Key, ex.Message);
            return OperationResult.Invalid(ex.Message);
        }
    }

    private async Task<RideExport> ExportRideAsync(IRideIntegration integration, Ride ride, string? bikeName, CancellationToken cancellationToken)
    {
        var now = _time.GetUtcNow().UtcDateTime;
        var export = await _exports.FindAsync(ride.Id, integration.Key, cancellationToken) ?? new RideExport { RideId = ride.Id, Integration = integration.Key };
        export.Attempts++;
        export.LastAttemptAt = now;

        try
        {
            var outcome = await integration.ExportAsync(ride, bikeName, cancellationToken);
            var uploaded = outcome.Status == RideExportStatus.Uploaded;
            export.Status = outcome.Status;
            export.RemoteId = outcome.RemoteId;
            export.Note = uploaded ? outcome.Note : null;
            export.Problem = uploaded ? null : outcome.Note;
            if (uploaded) export.ExportedAt = now;
        }
        catch (HttpRequestException ex)
        {
            LogExportFailed(ex, integration.Key, ride.Id);
            export.Status = RideExportStatus.Failed;
            export.Problem = ex.Message;
        }

        await _exports.SaveAsync(export, cancellationToken);
        return export;
    }

    private async Task<Dictionary<string, string>> BikeNamesAsync(CancellationToken cancellationToken) =>
        (await _bikes.GetAllAsync(cancellationToken)).ToDictionary(bike => bike.Id, bike => bike.Name);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Could not send ride {RideId} to {Integration}")]
    private partial void LogExportFailed(Exception exception, string integration, string rideId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "{Integration} needs signing in again: {Reason}")]
    private partial void LogSignInRequired(string integration, string reason);
}
