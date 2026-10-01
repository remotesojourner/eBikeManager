using EBikeManager.Application.Models;
using EBikeManager.Application.Models.Entities;

namespace EBikeManager.Application.Repositories.Interfaces;

public interface IBikeRepository
{
    Task<IReadOnlyList<Bike>> GetAllAsync(CancellationToken cancellationToken = default);
    Task ReplaceAsync(IReadOnlyList<Bike> bikes, CancellationToken cancellationToken = default);
    Task RenameAsync(string bikeId, string name, CancellationToken cancellationToken = default);
    Task SaveBridgeBatteryAsync(string bikeId, double percent, DateTime readAt, CancellationToken cancellationToken = default);
    Task SaveBridgeOdometerAsync(string bikeId, double kilometres, DateTime readAt, CancellationToken cancellationToken = default);
    Task SaveSnapshotAsync(string bikeId, BikeSnapshot snapshot, DateTime updatedAt, CancellationToken cancellationToken = default);
}
