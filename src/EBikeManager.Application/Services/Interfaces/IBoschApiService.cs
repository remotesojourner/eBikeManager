using EBikeManager.Application.Models;

namespace EBikeManager.Application.Services.Interfaces;

public interface IBoschApiService
{
    Task<IReadOnlyList<BoschBikeInfo>> GetBikesAsync(CancellationToken cancellationToken = default);

    Task<BoschActivityPage> GetActivitiesAsync(int page, int pageSize, CancellationToken cancellationToken = default);

    Task<byte[]?> DownloadFitAsync(string activityId, CancellationToken cancellationToken = default);
}
