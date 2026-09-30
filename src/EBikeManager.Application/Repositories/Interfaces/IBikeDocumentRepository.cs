using EBikeManager.Application.Models;
using EBikeManager.Application.Models.Entities;

namespace EBikeManager.Application.Repositories.Interfaces;

public interface IBikeDocumentRepository
{
    Task<IReadOnlyList<BikeDocumentInfo>> GetInfoAsync(CancellationToken cancellationToken = default);
    Task<BikeDocument?> FindAsync(string bikeId, string fileId, CancellationToken cancellationToken = default);
    Task SaveAsync(BikeDocument document, CancellationToken cancellationToken = default);
    Task KeepOnlyAsync(string bikeId, IReadOnlyCollection<string> fileIds, CancellationToken cancellationToken = default);
}
