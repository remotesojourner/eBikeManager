using EBikeManager.Application.Models.Entities;

namespace EBikeManager.Application.Repositories.Interfaces;

public interface IBikeRepository
{
    Task<IReadOnlyList<Bike>> GetAllAsync(CancellationToken cancellationToken = default);
    Task ReplaceAsync(IReadOnlyList<Bike> bikes, CancellationToken cancellationToken = default);
}
