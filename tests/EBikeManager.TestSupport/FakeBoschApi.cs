using EBikeManager.Application.Models;
using EBikeManager.Application.Services.Interfaces;

namespace EBikeManager.TestSupport;

internal sealed class FakeBoschApi : IBoschApiService
{
    public List<BoschBikeInfo> Bikes { get; } = [];

    public List<BoschActivity> Activities { get; } = [];

    public Dictionary<string, byte[]> FitFiles { get; } = [];

    public Dictionary<string, byte[]> GpxFiles { get; } = [];

    public Dictionary<string, string> Profiles { get; } = [];

    public Dictionary<string, string> StatesOfCharge { get; } = [];

    public Dictionary<string, string> Passes { get; } = [];

    public Dictionary<string, string> Locations { get; } = [];

    public bool? HasFlowPlus { get; set; } = false;

    public HashSet<string> FailingBikes { get; } = [];

    public Dictionary<string, byte[]> Pictures { get; } = [];

    public HashSet<string> FailingPictures { get; } = [];

    public List<Uri> DownloadedPictures { get; } = [];

    public Dictionary<string, byte[]> PassFiles { get; } = [];

    public HashSet<string> FailingPassFiles { get; } = [];

    public List<string> DownloadedPassFiles { get; } = [];

    public List<int> RequestedPages { get; } = [];

    public List<string> DownloadedFits { get; } = [];

    public List<string> DownloadedGpx { get; } = [];

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

    public Task<byte[]?> DownloadGpxAsync(string activityId, CancellationToken cancellationToken = default)
    {
        lock (DownloadedGpx) DownloadedGpx.Add(activityId);
        return Task.FromResult(GpxFiles.GetValueOrDefault(activityId));
    }

    public Task<string?> GetBikeProfileJsonAsync(string bikeId, CancellationToken cancellationToken = default) =>
        FailingBikes.Contains(bikeId)
            ? Task.FromException<string?>(new HttpRequestException("Bosch answered 503"))
            : Task.FromResult(Profiles.GetValueOrDefault(bikeId));

    public Task<string?> GetStateOfChargeJsonAsync(string bikeId, CancellationToken cancellationToken = default) =>
        Task.FromResult(StatesOfCharge.GetValueOrDefault(bikeId));

    public Task<string?> GetBikePassJsonAsync(string bikeId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Passes.GetValueOrDefault(bikeId));

    public Task<string?> GetLatestLocationJsonAsync(string bikeId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Locations.GetValueOrDefault(bikeId));

    public Task<bool?> HasFlowPlusAsync(CancellationToken cancellationToken = default) => Task.FromResult(HasFlowPlus);

    public Task<byte[]?> DownloadBikePictureAsync(Uri address, CancellationToken cancellationToken = default)
    {
        lock (DownloadedPictures) DownloadedPictures.Add(address);
        return FailingPictures.Contains(address.AbsoluteUri)
            ? Task.FromException<byte[]?>(new HttpRequestException("The picture server answered 503"))
            : Task.FromResult(Pictures.GetValueOrDefault(address.AbsoluteUri));
    }

    public Task<byte[]?> DownloadBikePassFileAsync(string bikeId, string fileId, CancellationToken cancellationToken = default)
    {
        lock (DownloadedPassFiles) DownloadedPassFiles.Add(fileId);
        return FailingPassFiles.Contains(fileId)
            ? Task.FromException<byte[]?>(new HttpRequestException("The bike pass answered 503"))
            : Task.FromResult(PassFiles.GetValueOrDefault(fileId));
    }

    public void AddBike(string bikeId, string name)
    {
        Bikes.Add(new BoschBikeInfo(bikeId, name));
        Profiles[bikeId] = BoschSamples.Profile(bikeId);
        Passes[bikeId] = BoschSamples.BikePass(bikeId);
        Pictures[BoschSamples.PictureUrl] = BoschSamples.Picture;
        PassFiles[BoschSamples.PhotoFileId] = BoschSamples.Picture;
        PassFiles[BoschSamples.InvoiceFileId] = BoschSamples.Pdf;
    }

    public BoschActivity AddRide(string id, string bikeId, DateTime start, bool withFit = true, string? title = null)
    {
        var activity = new BoschActivity(
            id, bikeId, title ?? $"Ride {id}", start, start.AddHours(1), "Europe/Berlin",
            DistanceMeters: 20_000, MovingSeconds: 3_300, CaloriesKcal: 410, ElevationGainMeters: 120,
            AverageSpeedKmh: 21.8, RiderEnergySharePercent: 62, AverageRiderPowerWatts: 110,
            AttributesJson: BoschSamples.RideSummary(title ?? id));
        Activities.Add(activity);
        if (withFit)
        {
            FitFiles[id] = TestFit.Create(start, TimeSpan.FromHours(1));
            GpxFiles[id] = TestGpx.Create(start);
        }
        return activity;
    }
}
