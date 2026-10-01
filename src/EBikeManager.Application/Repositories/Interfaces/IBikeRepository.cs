using EBikeManager.Application.Models;
using EBikeManager.Application.Models.Entities;

namespace EBikeManager.Application.Repositories.Interfaces;

public interface IBikeRepository
{
    Task<IReadOnlyList<Bike>> GetAllAsync(CancellationToken cancellationToken = default);
    Task ReplaceAsync(IReadOnlyList<Bike> bikes, CancellationToken cancellationToken = default);
    Task RenameAsync(string bikeId, string name, CancellationToken cancellationToken = default);
    Task SaveSnapshotAsync(string bikeId, BikeSnapshot snapshot, DateTime updatedAt, CancellationToken cancellationToken = default);
}
