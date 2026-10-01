using EBikeManager.Application.Data;
using EBikeManager.Application.Models;
using EBikeManager.Application.Models.Entities;
using EBikeManager.Application.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EBikeManager.Application.Repositories;

internal sealed class BikeRepository : IBikeRepository
{
    private readonly EBikeManagerDbContext _db;

    public BikeRepository(EBikeManagerDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<Bike>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await _db.Bikes.AsNoTracking().OrderBy(bike => bike.Name).ThenBy(bike => bike.Id).ToListAsync(cancellationToken);

    public async Task ReplaceAsync(IReadOnlyList<Bike> bikes, CancellationToken cancellationToken = default)
    {
        var stored = await _db.Bikes.ToDictionaryAsync(bike => bike.Id, cancellationToken);
        var kept = bikes.Select(bike => bike.Id).ToHashSet();

        _db.Bikes.RemoveRange(stored.Values.Where(bike => !kept.Contains(bike.Id)));
        foreach (var bike in bikes)
        {
            if (stored.TryGetValue(bike.Id, out var existing)) existing.Model = bike.Model;
            else _db.Bikes.Add(bike);
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    public Task RenameAsync(string bikeId, string name, CancellationToken cancellationToken = default) =>
        _db.Bikes
            .Where(bike => bike.Id == bikeId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(bike => bike.Name, name), cancellationToken);

    public Task SaveBridgeBatteryAsync(string bikeId, double percent, DateTime readAt, CancellationToken cancellationToken = default) =>
        _db.Bikes
            .Where(bike => bike.Id == bikeId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(bike => bike.BridgeBatteryPercent, percent).SetProperty(bike => bike.BridgeBatteryAt, readAt), cancellationToken);

    public Task SaveBridgeOdometerAsync(string bikeId, double kilometres, DateTime readAt, CancellationToken cancellationToken = default) =>
        _db.Bikes
            .Where(bike => bike.Id == bikeId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(bike => bike.BridgeOdometerKm, kilometres).SetProperty(bike => bike.BridgeOdometerAt, readAt), cancellationToken);

    public async Task SaveSnapshotAsync(string bikeId, BikeSnapshot snapshot, DateTime updatedAt, CancellationToken cancellationToken = default)
    {
        if (await _db.Bikes.SingleOrDefaultAsync(bike => bike.Id == bikeId, cancellationToken) is not { } bike) return;

        bike.ProfileJson = snapshot.ProfileJson;
        bike.StateOfChargeJson = snapshot.StateOfChargeJson;
        bike.PassJson = snapshot.PassJson;
        bike.LocationJson = snapshot.LocationJson;
        bike.HasFlowPlus = snapshot.HasFlowPlus;
        bike.DetailsUpdatedAt = updatedAt;
        await _db.SaveChangesAsync(cancellationToken);
    }
}
