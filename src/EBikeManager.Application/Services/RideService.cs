using EBikeManager.Application.Models;
using EBikeManager.Application.Models.Dtos;
using EBikeManager.Application.Repositories.Interfaces;
using EBikeManager.Application.Resources;

namespace EBikeManager.Application.Services;

public sealed class RideService
{
    private readonly IRideRepository _rides;
    private readonly FitArchiveService _archive;

    public RideService(IRideRepository rides, FitArchiveService archive)
    {
        _rides = rides;
        _archive = archive;
    }

    public async Task<IReadOnlyList<RideDto>> GetRidesAsync(int? limit = null, CancellationToken cancellationToken = default) =>
        (await _rides.GetListAsync(limit, cancellationToken)).Select(RideDto.From).ToList();

    public Task<RideTotalsDto> GetTotalsAsync(DateTime? sinceUtc, CancellationToken cancellationToken = default) =>
        _rides.TotalsSinceAsync(sinceUtc, cancellationToken);

    public async Task<OperationResult<ExportFile>> GetFitFileAsync(string id, CancellationToken cancellationToken = default)
    {
        if (await _rides.FindAsync(id, cancellationToken) is not { } ride) return OperationResult.NotFound(ApplicationStrings.RideNotFound);
        if (ride.FitPath == null) return OperationResult.NotFound(ApplicationStrings.RideNoBackup);
        if (await _archive.ReadAsync(ride.FitPath, cancellationToken) is not { } content) return OperationResult.NotFound(ApplicationStrings.RideFileMissing);

        return OperationResult.Ok(new ExportFile(Path.GetFileName(ride.FitPath), content));
    }
}
