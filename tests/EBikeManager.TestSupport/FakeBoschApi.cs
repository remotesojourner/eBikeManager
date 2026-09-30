using EBikeManager.Application.Models;
using EBikeManager.Application.Services.Interfaces;

namespace EBikeManager.TestSupport;

internal sealed class FakeBoschApi : IBoschApiService
{
    public List<BoschBikeInfo> Bikes { get; } = [];

    public List<BoschActivity> Activities { get; } = [];

    public Dictionary<string, byte[]> FitFiles { get; } = [];

    public List<int> RequestedPages { get; } = [];

    public List<string> DownloadedFits { get; } = [];

    public Task<IReadOnlyList<BoschBikeInfo>> GetBikesAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<BoschBikeInfo>>([.. Bikes]);

    public Task<BoschActivityPage> GetActivitiesAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        lock (RequestedPages) RequestedPages.Add(page);
        var sorted = Activities.OrderByDescending(activity => activity.StartTime).ToList();
        var totalPages = (int)Math.Ceiling(sorted.Count / (double)pageSize);
        return Task.FromResult(new BoschActivityPage(sorted.Skip(page * pageSize).Take(pageSize).ToList(), totalPages));
    }

    public Task<byte[]?> DownloadFitAsync(string activityId, CancellationToken cancellationToken = default)
    {
        lock (DownloadedFits) DownloadedFits.Add(activityId);
        return Task.FromResult(FitFiles.GetValueOrDefault(activityId));
    }

    public BoschActivity AddRide(string id, string bikeId, DateTime start, bool withFit = true, string? title = null)
    {
        var activity = new BoschActivity(
            id, bikeId, title ?? $"Ride {id}", start, start.AddHours(1), "Europe/Berlin",
            DistanceMeters: 20_000, MovingSeconds: 3_300, CaloriesKcal: 410, ElevationGainMeters: 120,
            AverageSpeedKmh: 21.8, RiderEnergySharePercent: 62, AverageRiderPowerWatts: 110,
            AttributesJson: $$"""{"title":"{{title ?? id}}"}""");
        Activities.Add(activity);
        if (withFit) FitFiles[id] = TestFit.Create(start, TimeSpan.FromHours(1));
        return activity;
    }
}
