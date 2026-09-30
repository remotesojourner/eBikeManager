using EBikeManager.Application.Data;
using EBikeManager.Application.Enums;
using EBikeManager.Application.Models.Dtos;
using EBikeManager.Application.Models.Entities;
using EBikeManager.Application.Repositories.Interfaces;
using EBikeManager.Application.Utils;
using Microsoft.EntityFrameworkCore;

namespace EBikeManager.Application.Repositories;

internal sealed class RideExportRepository : IRideExportRepository
{
    private readonly EBikeManagerDbContext _db;

    public RideExportRepository(EBikeManagerDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<Ride>> GetRidesToExportAsync(string integration, DateTime? fromUtc, int limit, CancellationToken cancellationToken = default) =>
        await _db.Rides.AsNoTracking()
            .Where(ride => ride.EndTime != null && (fromUtc == null || ride.StartTime >= fromUtc))
            .Where(ride => !_db.RideExports.Any(export => export.RideId == ride.Id && export.Integration == integration
                && (export.Status == RideExportStatus.Uploaded || export.Status == RideExportStatus.WatchRecorded)))
            .OrderBy(ride => ride.StartTime)
            .ThenBy(ride => ride.Id)
            .Take(limit)
            .ToListAsync(cancellationToken);

    public Task<RideExport?> FindAsync(string rideId, string integration, CancellationToken cancellationToken = default) =>
        _db.RideExports.SingleOrDefaultAsync(export => export.RideId == rideId && export.Integration == integration, cancellationToken);

    public async Task SaveAsync(RideExport rideExport, CancellationToken cancellationToken = default)
    {
        if (_db.Entry(rideExport).State == EntityState.Detached) _db.RideExports.Add(rideExport);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public Task<IReadOnlyList<RideExportDto>> GetAllAsync(string integration, CancellationToken cancellationToken = default) =>
        ReadAsync(_db.RideExports.Where(export => export.Integration == integration), cancellationToken);

    public Task<IReadOnlyList<RideExportDto>> GetForRideAsync(string rideId, CancellationToken cancellationToken = default) =>
        ReadAsync(_db.RideExports.Where(export => export.RideId == rideId), cancellationToken);

    public async Task<RideExportCountsDto> CountAsync(string integration, CancellationToken cancellationToken = default)
    {
        var rows = await _db.RideExports.AsNoTracking()
            .Where(export => export.Integration == integration)
            .Select(export => new { export.Status, HasNote = export.Note != null })
            .ToListAsync(cancellationToken);

        return new RideExportCountsDto(
            rows.Count(row => row.Status == RideExportStatus.Uploaded),
            rows.Count(row => row.Status != RideExportStatus.Uploaded),
            rows.Count(row => row.HasNote));
    }

    private async Task<IReadOnlyList<RideExportDto>> ReadAsync(IQueryable<RideExport> exports, CancellationToken cancellationToken)
    {
        var rows = await (
                from export in exports.AsNoTracking()
                join ride in _db.Rides.AsNoTracking() on export.RideId equals ride.Id
                orderby ride.StartTime descending
                select new { Export = export, ride.Title, ride.StartTime, ride.TimeZone })
            .ToListAsync(cancellationToken);

        return [.. rows.Select(row => new RideExportDto(
            row.Export.RideId,
            row.Title,
            TimeZones.ToRideLocal(row.StartTime, row.TimeZone),
            row.Export.Integration,
            row.Export.Status,
            row.Export.ExportedAt,
            row.Export.Note,
            row.Export.Problem))];
    }
}
