using EBikeManager.Application.Configuration;
using EBikeManager.Application.Models;
using EBikeManager.Application.Models.Entities;
using EBikeManager.Application.Repositories.Interfaces;
using EBikeManager.Application.Resources;
using EBikeManager.Application.Services.Interfaces;
using EBikeManager.Application.Utils;
using Microsoft.Extensions.Logging;

namespace EBikeManager.Application.Services;

public sealed partial class RideSyncService
{
    public const int PageSize = 30;

    public static readonly TimeSpan RecheckWindow = TimeSpan.FromDays(7);

    private readonly IBoschApiService _bosch;
    private readonly IRideRepository _rides;
    private readonly IBikeRepository _bikes;
    private readonly ISettingsRepository _settings;
    private readonly FitArchiveService _archive;
    private readonly SyncStateService _state;
    private readonly TimeProvider _time;
    private readonly ILogger<RideSyncService> _logger;

    public RideSyncService(
        IBoschApiService bosch,
        IRideRepository rides,
        IBikeRepository bikes,
        ISettingsRepository settings,
        FitArchiveService archive,
        SyncStateService state,
        TimeProvider time,
        ILogger<RideSyncService> logger)
    {
        _bosch = bosch;
        _rides = rides;
        _bikes = bikes;
        _settings = settings;
        _archive = archive;
        _state = state;
        _time = time;
        _logger = logger;
    }

    public TimeSpan DownloadDelay { get; set; } = TimeSpan.FromMilliseconds(500);

    public async Task<SyncRunResult> RunAsync(CancellationToken cancellationToken = default)
    {
        var bikeIds = (await _bikes.GetAllAsync(cancellationToken)).Select(bike => bike.Id).ToHashSet();
        if (bikeIds.Count == 0) throw new InvalidOperationException(ApplicationStrings.SyncNotSetUp);

        var fullScanRequested = (await _settings.GetAsync(cancellationToken)).Bosch.FullScanRequested;
        var rides = await _rides.GetAllForUpdateAsync(cancellationToken);
        var fullScan = rides.Count == 0 || fullScanRequested;

        var (ridesChecked, newRides) = await ReadRideListAsync(rides, bikeIds, fullScan, cancellationToken);
        await _rides.SaveChangesAsync(cancellationToken);
        if (fullScanRequested) await _settings.SaveAsync(new Dictionary<string, string> { [SettingDefinitions.BoschFullScan] = "false" }, cancellationToken);

        var pending = rides.Values
            .Where(ride => bikeIds.Contains(ride.BikeId) && ride.FitPath == null && !ride.FitUnavailable && ride.EndTime != null)
            .OrderByDescending(ride => ride.StartTime)
            .ToList();

        var saved = 0;
        var problems = new List<string>();
        for (var index = 0; index < pending.Count; index++)
        {
            var ride = pending[index];
            _state.ReportProgress(ApplicationStrings.Format(ApplicationStrings.SyncProgressDownloading, index + 1, pending.Count));
            try
            {
                if (await BackUpFitAsync(ride, cancellationToken)) saved++;
            }
            catch (Exception ex) when (ex is HttpRequestException or InvalidDataException or IOException)
            {
                LogFitBackupFailed(ex, ride.Id);
                ride.FitError = ex.Message;
                problems.Add($"{ride.Title ?? ride.Id}: {ex.Message}");
            }

            await _rides.SaveChangesAsync(cancellationToken);
            if (index < pending.Count - 1) await Task.Delay(DownloadDelay, _time, cancellationToken);
        }

        LogFinished(ridesChecked, newRides, saved, problems.Count);
        return new SyncRunResult(ridesChecked, newRides, saved, problems);
    }

    private async Task<(int Checked, int New)> ReadRideListAsync(Dictionary<string, Ride> rides, HashSet<string> bikeIds, bool fullScan, CancellationToken cancellationToken)
    {
        var now = _time.GetUtcNow().UtcDateTime;
        var recheckFrom = now - RecheckWindow;
        var ridesChecked = 0;
        var newRides = 0;

        for (var page = 0; ; page++)
        {
            _state.ReportProgress(ApplicationStrings.Format(ApplicationStrings.SyncProgressReadingList, page + 1));
            var result = await _bosch.GetActivitiesAsync(page, PageSize, cancellationToken);

            var nothingNew = true;
            foreach (var activity in result.Activities.Where(activity => activity.BikeId != null && bikeIds.Contains(activity.BikeId)))
            {
                ridesChecked++;
                if (!rides.TryGetValue(activity.Id, out var ride))
                {
                    ride = new Ride { Id = activity.Id, BikeId = activity.BikeId!, FirstSeenAt = now };
                    _rides.Add(ride);
                    rides[ride.Id] = ride;
                    newRides++;
                    nothingNew = false;
                }
                else if (activity.StartTime >= recheckFrom)
                {
                    nothingNew = false;
                }

                Apply(ride, activity);
            }

            if (result.Activities.Count == 0 || page + 1 >= result.TotalPages || (!fullScan && nothingNew)) return (ridesChecked, newRides);
        }
    }

    private async Task<bool> BackUpFitAsync(Ride ride, CancellationToken cancellationToken)
    {
        var fit = await _bosch.DownloadFitAsync(ride.Id, cancellationToken);
        if (fit == null)
        {
            ride.FitUnavailable = true;
            ride.FitError = ApplicationStrings.FitNoFile;
            return false;
        }

        var summary = FitDecoder.ReadSummary(fit);
        var relativePath = FitArchiveService.RelativePathFor(ride);
        ride.FitSha256 = await _archive.SaveAsync(relativePath, fit, ride.SummaryJson, cancellationToken);
        ride.FitPath = relativePath;
        ride.FitSizeBytes = fit.Length;
        ride.FitDownloadedAt = _time.GetUtcNow().UtcDateTime;
        ride.FitError = null;
        ride.FitTimerSeconds = summary.TimerSeconds;
        ride.FitDistanceMeters = summary.DistanceMeters;
        ride.FitAveragePowerWatts = summary.AveragePowerWatts;
        ride.FitHasGps = summary.HasGps;
        return true;
    }

    private static void Apply(Ride ride, BoschActivity activity)
    {
        ride.BikeId = activity.BikeId ?? ride.BikeId;
        ride.Title = activity.Title;
        ride.StartTime = activity.StartTime;
        ride.EndTime = activity.EndTime;
        ride.TimeZone = activity.TimeZone;
        ride.DistanceMeters = activity.DistanceMeters;
        ride.MovingSeconds = activity.MovingSeconds;
        ride.CaloriesKcal = activity.CaloriesKcal;
        ride.ElevationGainMeters = activity.ElevationGainMeters;
        ride.AverageSpeedKmh = activity.AverageSpeedKmh;
        ride.RiderEnergySharePercent = activity.RiderEnergySharePercent;
        ride.AverageRiderPowerWatts = activity.AverageRiderPowerWatts;
        ride.SummaryJson = activity.AttributesJson;
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Could not back up the FIT file of ride {RideId}")]
    private partial void LogFitBackupFailed(Exception exception, string rideId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Sync finished: {Checked} rides checked, {New} new, {Saved} FIT files saved, {Problems} problems")]
    private partial void LogFinished(int @checked, int @new, int saved, int problems);
}
