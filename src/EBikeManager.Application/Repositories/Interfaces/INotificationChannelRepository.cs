using EBikeManager.Application.Models.Entities;

namespace EBikeManager.Application.Repositories.Interfaces;

public interface INotificationChannelRepository
{
    Task<IReadOnlyList<NotificationChannel>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<NotificationChannel?> FindAsync(string id, CancellationToken cancellationToken = default);

    Task<string> CreateAsync(string type, string displayName, string data, CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(string id, string? displayName, string data, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default);

    Task SaveActivityAsync(string id, bool failed, DateTime at, CancellationToken cancellationToken = default);
}
