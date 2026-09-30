using EBikeManager.Application.Models;
using EBikeManager.Application.Models.Entities;

namespace EBikeManager.Application.Services.Interfaces;

public interface IRideIntegration
{
    string Key { get; }

    string DisplayName { get; }

    Task<ExportWindow?> GetExportWindowAsync(CancellationToken cancellationToken = default);

    Task<RideExportOutcome> ExportAsync(Ride ride, string? bikeName, CancellationToken cancellationToken = default);
}
