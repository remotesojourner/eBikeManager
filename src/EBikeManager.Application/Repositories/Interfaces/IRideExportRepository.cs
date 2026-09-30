using EBikeManager.Application.Models.Dtos;
using EBikeManager.Application.Models.Entities;

namespace EBikeManager.Application.Repositories.Interfaces;

public interface IRideExportRepository
{
    Task<IReadOnlyList<Ride>> GetRidesToExportAsync(string integration, DateTime? fromUtc, int limit, CancellationToken cancellationToken = default);
    Task<RideExport?> FindAsync(string rideId, string integration, CancellationToken cancellationToken = default);
    Task SaveAsync(RideExport rideExport, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RideExportDto>> GetAllAsync(string integration, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RideExportDto>> GetForRideAsync(string rideId, CancellationToken cancellationToken = default);
    Task<RideExportCountsDto> CountAsync(string integration, CancellationToken cancellationToken = default);
}
