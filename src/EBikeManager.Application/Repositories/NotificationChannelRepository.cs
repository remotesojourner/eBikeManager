using EBikeManager.Application.Data;
using EBikeManager.Application.Models.Entities;
using EBikeManager.Application.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EBikeManager.Application.Repositories;

internal sealed class NotificationChannelRepository : INotificationChannelRepository
{
    private const string Untitled = "Untitled";

    private readonly EBikeManagerDbContext _db;

    public NotificationChannelRepository(EBikeManagerDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<NotificationChannel>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await _db.NotificationChannels.AsNoTracking().OrderBy(channel => channel.DisplayName).ThenBy(channel => channel.Id).ToListAsync(cancellationToken);

    public Task<NotificationChannel?> FindAsync(string id, CancellationToken cancellationToken = default) =>
        _db.NotificationChannels.AsNoTracking().FirstOrDefaultAsync(channel => channel.Id == id, cancellationToken);

    public async Task<string> CreateAsync(string type, string displayName, string data, CancellationToken cancellationToken = default)
    {
        var channel = new NotificationChannel { Id = Guid.NewGuid().ToString("N")[..12], Type = type, DisplayName = NameOrDefault(displayName), Data = data };
        _db.NotificationChannels.Add(channel);
        await _db.SaveChangesAsync(cancellationToken);
        return channel.Id;
    }

    public async Task<bool> UpdateAsync(string id, string? displayName, string data, CancellationToken cancellationToken = default)
    {
        if (await _db.NotificationChannels.FirstOrDefaultAsync(channel => channel.Id == id, cancellationToken) is not { } channel) return false;

        if (!string.IsNullOrWhiteSpace(displayName)) channel.DisplayName = displayName.Trim();
        channel.Data = data;
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default) =>
        await _db.NotificationChannels.Where(channel => channel.Id == id).ExecuteDeleteAsync(cancellationToken) > 0;

    public Task SaveActivityAsync(string id, bool failed, DateTime at, CancellationToken cancellationToken = default) =>
        _db.NotificationChannels.Where(channel => channel.Id == id)
            .ExecuteUpdateAsync(update => update.SetProperty(channel => channel.LastActivity, at).SetProperty(channel => channel.ActivityFailed, failed), cancellationToken);

    private static string NameOrDefault(string displayName) => string.IsNullOrWhiteSpace(displayName) ? Untitled : displayName.Trim();
}
