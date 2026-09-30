using EBikeManager.Application.Models;

namespace EBikeManager.Application.Services.Interfaces;

public interface IBoschApiService
{
    Task<IReadOnlyList<BoschBikeInfo>> GetBikesAsync(CancellationToken cancellationToken = default);

    Task<BoschActivityPage> GetActivitiesAsync(int page, int pageSize, CancellationToken cancellationToken = default);

    Task<byte[]?> DownloadFitAsync(string activityId, CancellationToken cancellationToken = default);

    Task<byte[]?> DownloadGpxAsync(string activityId, CancellationToken cancellationToken = default);

    Task<string?> GetBikeProfileJsonAsync(string bikeId, CancellationToken cancellationToken = default);

    Task<string?> GetStateOfChargeJsonAsync(string bikeId, CancellationToken cancellationToken = default);

    Task<string?> GetBikePassJsonAsync(string bikeId, CancellationToken cancellationToken = default);

    Task<string?> GetLatestLocationJsonAsync(string bikeId, CancellationToken cancellationToken = default);

    Task<bool?> HasFlowPlusAsync(CancellationToken cancellationToken = default);

    Task<byte[]?> DownloadBikePictureAsync(Uri address, CancellationToken cancellationToken = default);

    Task<byte[]?> DownloadBikePassFileAsync(string bikeId, string fileId, CancellationToken cancellationToken = default);
}
