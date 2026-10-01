using EBikeManager.Application.Data;
using EBikeManager.Application.Models.Dtos;
using EBikeManager.Application.Models.Entities;
using EBikeManager.Application.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EBikeManager.Application.Repositories;

internal sealed class RideRepository : IRideRepository
{
    private readonly EBikeManagerDbContext _db;

    public RideRepository(EBikeManagerDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<Ride>> GetListAsync(int? limit = null, CancellationToken cancellationToken = default)
    {
        var query = _db.Rides.AsNoTracking().OrderByDescending(ride => ride.StartTime).ThenBy(ride => ride.Id);
        return limit is { } count
            ? await query.Take(count).ToListAsync(cancellationToken)
            : await query.ToListAsync(cancellationToken);
    }

    public Task<Ride?> FindAsync(string id, CancellationToken cancellationToken = default) =>
        _db.Rides.AsNoTracking().SingleOrDefaultAsync(ride => ride.Id == id, cancellationToken);

    public async Task<IReadOnlyList<string>> GetRecentSummariesAsync(string bikeId, int count, CancellationToken cancellationToken = default) =>
        await _db.Rides.AsNoTracking()
            .Where(ride => ride.BikeId == bikeId)
            .OrderByDescending(ride => ride.StartTime)
            .ThenBy(ride => ride.Id)
            .Take(count)
            .Select(ride => ride.SummaryJson)
            .ToListAsync(cancellationToken);

    public async Task<RideTotalsDto> TotalsSinceAsync(DateTime? sinceUtc, CancellationToken cancellationToken = default)
    {
        var rides = _db.Rides.AsNoTracking().Where(ride => sinceUtc == null || ride.StartTime >= sinceUtc);
        var totals = await rides
            .GroupBy(_ => 1)
            .Select(group => new RideTotalsDto(
                group.Count(),
                group.Sum(ride => (double?)ride.DistanceMeters) ?? 0,
                group.Sum(ride => (double?)ride.MovingSeconds) ?? 0,
                group.Sum(ride => ride.CaloriesKcal) ?? 0,
                group.Sum(ride => (double?)ride.ElevationGainMeters) ?? 0))
            .SingleOrDefaultAsync(cancellationToken);
        return totals ?? RideTotalsDto.None;
    }

    public Task<Dictionary<string, Ride>> GetAllForUpdateAsync(CancellationToken cancellationToken = default) =>
        _db.Rides.ToDictionaryAsync(ride => ride.Id, cancellationToken);

    public Task RenameBikeAsync(string bikeId, string bikeName, string? bikeModel, CancellationToken cancellationToken = default) =>
        _db.Rides
            .Where(ride => ride.BikeId == bikeId && (ride.BikeName != bikeName || ride.BikeModel != bikeModel))
            .ExecuteUpdateAsync(setters => setters.SetProperty(ride => ride.BikeName, bikeName).SetProperty(ride => ride.BikeModel, bikeModel), cancellationToken);

    public void Add(Ride ride) => _db.Rides.Add(ride);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) => _db.SaveChangesAsync(cancellationToken);
}
