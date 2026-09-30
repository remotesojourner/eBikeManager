using EBikeManager.Application.Models.Entities;

namespace EBikeManager.Application.Repositories.Interfaces;

public interface IBikePictureRepository
{
    Task<string?> GetSourceUrlAsync(string bikeId, CancellationToken cancellationToken = default);
    Task<IReadOnlyDictionary<string, DateTime>> GetSavedTimesAsync(CancellationToken cancellationToken = default);
    Task<BikePicture?> FindAsync(string bikeId, CancellationToken cancellationToken = default);
    Task SaveAsync(BikePicture picture, CancellationToken cancellationToken = default);
}
