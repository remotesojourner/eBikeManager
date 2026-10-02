using System.Data.Common;
using EBikeManager.Application.Exceptions;
using EBikeManager.Application.Models;
using EBikeManager.Application.Models.Dtos;
using EBikeManager.Application.Models.Entities;
using EBikeManager.Application.Models.Events;
using EBikeManager.Application.Repositories.Interfaces;
using EBikeManager.Application.Resources;
using EBikeManager.Application.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EBikeManager.Application.Services;

public sealed partial class SyncRunService
{
    public const string BoschService = "Bosch eBike Flow";

    private readonly SyncStateService _state;
    private readonly IServiceScopeFactory _scopes;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly ISettingsRepository _settings;
    private readonly ICurrentAccessService _access;
    private readonly NotificationStateService _notifications;
    private readonly INotificationDispatchService _dispatcher;
    private readonly IRideRepository _rides;
    private readonly ILogger<SyncRunService> _logger;

    public SyncRunService(
        SyncStateService state,
        IServiceScopeFactory scopes,
        IHostApplicationLifetime lifetime,
        ISettingsRepository settings,
        ICurrentAccessService access,
        NotificationStateService notifications,
        INotificationDispatchService dispatcher,
        IRideRepository rides,
        ILogger<SyncRunService> logger)
    {
        _state = state;
        _scopes = scopes;
        _lifetime = lifetime;
        _settings = settings;
        _access = access;
        _notifications = notifications;
        _dispatcher = dispatcher;
        _rides = rides;
        _logger = logger;
    }

    public async Task<OperationResult> StartManualSyncAsync(CancellationToken cancellationToken = default)
    {
        if (!_access.HasFullAccess) return OperationResult.Denied();
        if (!(await _settings.GetAsync(cancellationToken)).SetupCompleted) return OperationResult.Invalid(ApplicationStrings.SyncNotSetUp);
        if (!_state.TryStart(ApplicationStrings.SyncProgressStarting)) return OperationResult.Conflict(ApplicationStrings.SyncAlreadyRunning);

        var stopping = _lifetime.ApplicationStopping;
        _ = Task.Run(async () =>
        {
            using var scope = _scopes.CreateScope();
            await scope.ServiceProvider.GetRequiredService<SyncRunService>().RunLockedAsync(stopping);
        }, CancellationToken.None);
        return OperationResult.Ok();
    }

    public async Task RunScheduledAsync(CancellationToken cancellationToken = default)
    {
        if (!(await _settings.GetAsync(cancellationToken)).SetupCompleted) return;
        if (!_state.TryStart(ApplicationStrings.SyncProgressStarting))
        {
            LogScheduledSyncSkipped();
            return;
        }

        await RunLockedAsync(cancellationToken);
    }

    private async Task RunLockedAsync(CancellationToken cancellationToken)
    {
        SyncRunResult? result = null;
        IntegrationRunResult? uploads = null;
        string? error = null;
        var boschSignInRequired = false;
        try
        {
            using var scope = _scopes.CreateScope();
            var rides = await scope.ServiceProvider.GetRequiredService<RideSyncService>().RunAsync(cancellationToken);
            _state.ReportProgress(ApplicationStrings.SyncProgressBikeDetails);
            var bikeProblems = await scope.ServiceProvider.GetRequiredService<BikeDetailsSyncService>().RefreshAsync(cancellationToken);
            _state.ReportProgress(ApplicationStrings.SyncProgressIntegrations);
            uploads = await scope.ServiceProvider.GetRequiredService<IntegrationSyncService>().RunAsync(cancellationToken);
            result = rides with { Problems = [.. rides.Problems, .. bikeProblems, .. uploads.Problems], RidesUploaded = uploads.Uploaded };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            error = ApplicationStrings.SyncStoppedByShutdown;
        }
        catch (BoschReauthRequiredException)
        {
            error = ApplicationStrings.BoschReconnectNeeded;
            boschSignInRequired = true;
        }
        catch (Exception ex) when (ex is HttpRequestException or IOException or InvalidOperationException or DbException)
        {
            LogSyncFailed(ex);
            error = ex.Message;
        }

        _state.Finish(result, error);
        if (!cancellationToken.IsCancellationRequested) await NotifyAsync(result, uploads, error, boschSignInRequired, cancellationToken);
    }

    private async Task NotifyAsync(SyncRunResult? result, IntegrationRunResult? uploads, string? error, bool boschSignInRequired, CancellationToken cancellationToken)
    {
        try
        {
            foreach (var notification in await EventsAsync(result, uploads, error, boschSignInRequired, cancellationToken))
            {
                await _dispatcher.PublishAsync(notification, cancellationToken);
            }
        }
        catch (DbException ex)
        {
            LogNotifyFailed(ex);
        }
    }

    private async Task<List<NotificationEvent>> EventsAsync(SyncRunResult? result, IntegrationRunResult? uploads, string? error, bool boschSignInRequired, CancellationToken cancellationToken)
    {
        var events = new List<NotificationEvent>();
        if (boschSignInRequired)
        {
            if (_notifications.SignInNeeded(BoschService)) events.Add(new SignInRequired(BoschService));
            return events;
        }

        if (result == null)
        {
            if (error != null && _notifications.SyncFailed()) events.Add(new SyncFailed(error));
            return events;
        }

        if (_notifications.SyncWorked()) events.Add(new SyncRestored());

        var finished = new List<Ride>();
        foreach (var id in result.FinishedRideIds ?? [])
        {
            if (await _rides.FindAsync(id, cancellationToken) is { } ride) finished.Add(ride);
        }

        events.AddRange(finished.OrderBy(ride => ride.StartTime).Select(ride => new RideSynced(RideDto.From(ride))));
        events.AddRange(uploads?.FailedUploads ?? []);
        events.AddRange((uploads?.SignInsRequired ?? []).Where(_notifications.SignInNeeded).Select(service => new SignInRequired(service)));
        return events;
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "The notifications for a finished sync could not be sent")]
    private partial void LogNotifyFailed(Exception exception);

    [LoggerMessage(Level = LogLevel.Error, Message = "The sync failed")]
    private partial void LogSyncFailed(Exception exception);

    [LoggerMessage(Level = LogLevel.Information, Message = "Skipped a scheduled sync because another one is still running")]
    private partial void LogScheduledSyncSkipped();
}
