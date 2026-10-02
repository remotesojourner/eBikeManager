using EBikeManager.Application.Enums;
using EBikeManager.Application.Models;
using EBikeManager.Application.Models.Dtos;
using EBikeManager.Application.Models.Entities;
using EBikeManager.Application.Repositories.Interfaces;
using EBikeManager.Application.Resources;
using EBikeManager.Application.Utils;

namespace EBikeManager.Application.Services;

public sealed class RideService
{
    private const int RoutePreviewCandidates = 10;

    private readonly IRideRepository _rides;
    private readonly IRideExportRepository _exports;
    private readonly FitArchiveService _archive;

    public RideService(IRideRepository rides, IRideExportRepository exports, FitArchiveService archive)
    {
        _rides = rides;
        _exports = exports;
        _archive = archive;
    }

    public async Task<IReadOnlyList<RideDto>> GetRidesAsync(int? limit = null, CancellationToken cancellationToken = default) =>
        (await _rides.GetListAsync(limit, cancellationToken)).Select(RideDto.From).ToList();

    public Task<RideTotalsDto> GetTotalsAsync(DateTime? sinceUtc, CancellationToken cancellationToken = default) =>
        _rides.TotalsSinceAsync(sinceUtc, cancellationToken);

    public async Task<OperationResult<RideDetailDto>> GetDetailAsync(string id, UnitSystem units, CancellationToken cancellationToken = default)
    {
        if (await _rides.FindAsync(id, cancellationToken) is not { } ride) return OperationResult.NotFound(ApplicationStrings.RideNotFound);

        var (track, problem) = await ReadTrackAsync(ride, UnitConversion.SplitMetres(units), cancellationToken);
        var exports = await _exports.GetForRideAsync(ride.Id, cancellationToken);
        return OperationResult.Ok(RideDetailParser.Parse(ride, track, problem) with { Exports = exports });
    }

    public Task<IReadOnlyList<RideExportDto>> GetExportsAsync(string id, CancellationToken cancellationToken = default) =>
        _exports.GetForRideAsync(id, cancellationToken);

    public async Task<IReadOnlyList<double[]>> GetLatestRouteAsync(CancellationToken cancellationToken = default)
    {
        foreach (var ride in await _rides.GetListAsync(RoutePreviewCandidates, cancellationToken))
        {
            if (ride.FitPath == null) continue;
            if ((await ReadTrackAsync(ride, UnitConversion.MetresPerKilometre, cancellationToken)).Track is { HasRoute: true } track) return track.Route;
        }

        return [];
    }

    public async Task<OperationResult<ExportFile>> GetGpxFileAsync(string id, CancellationToken cancellationToken = default)
    {
        if (await _rides.FindAsync(id, cancellationToken) is not { } ride) return OperationResult.NotFound(ApplicationStrings.RideNotFound);
        if (ride.GpxPath == null) return OperationResult.NotFound(ApplicationStrings.RideNoGpxBackup);
        if (await _archive.ReadAsync(ride.GpxPath, cancellationToken) is not { } content) return OperationResult.NotFound(ApplicationStrings.RideFileMissing);

        return OperationResult.Ok(new ExportFile(Path.GetFileName(ride.GpxPath), content));
    }

    public async Task<OperationResult<ExportFile>> GetFitFileAsync(string id, CancellationToken cancellationToken = default)
    {
        if (await _rides.FindAsync(id, cancellationToken) is not { } ride) return OperationResult.NotFound(ApplicationStrings.RideNotFound);
        if (ride.FitPath == null) return OperationResult.NotFound(ApplicationStrings.RideNoBackup);
        if (await _archive.ReadAsync(ride.FitPath, cancellationToken) is not { } content) return OperationResult.NotFound(ApplicationStrings.RideFileMissing);

        return OperationResult.Ok(new ExportFile(Path.GetFileName(ride.FitPath), content));
    }

    private async Task<(RideTrackDto? Track, string? Problem)> ReadTrackAsync(Ride ride, double metresPerSplit, CancellationToken cancellationToken)
    {
        if (ride.FitPath == null) return (null, ride.FitUnavailable ? ApplicationStrings.RideTrackNotFromBosch : ApplicationStrings.RideTrackNotYet);
        if (await _archive.ReadAsync(ride.FitPath, cancellationToken) is not { } fit) return (null, ApplicationStrings.RideFileMissing);

        try
        {
            return (RideTrackBuilder.Build(FitDecoder.ReadRecords(fit), metresPerSplit), null);
        }
        catch (InvalidDataException ex)
        {
            return (null, ex.Message);
        }
    }
}
