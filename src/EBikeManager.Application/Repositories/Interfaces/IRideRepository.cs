using EBikeManager.Application.Models.Dtos;
using EBikeManager.Application.Models.Entities;

namespace EBikeManager.Application.Repositories.Interfaces;

public interface IRideRepository
{
    Task<IReadOnlyList<Ride>> GetListAsync(int? limit = null, CancellationToken cancellationToken = default);
    Task<Ride?> FindAsync(string id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetRecentSummariesAsync(string bikeId, int count, CancellationToken cancellationToken = default);
    Task<RideTotalsDto> TotalsSinceAsync(DateTime? sinceUtc, CancellationToken cancellationToken = default);
    Task<Dictionary<string, Ride>> GetAllForUpdateAsync(CancellationToken cancellationToken = default);
    void Add(Ride ride);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
