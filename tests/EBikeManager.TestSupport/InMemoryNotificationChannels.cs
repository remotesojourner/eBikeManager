using EBikeManager.Application.Models.Entities;
using EBikeManager.Application.Repositories.Interfaces;

namespace EBikeManager.TestSupport;

internal sealed class InMemoryNotificationChannels(List<NotificationChannel> channels) : INotificationChannelRepository
{
    public List<bool> ActivityErrors { get; } = [];

    public Task<IReadOnlyList<NotificationChannel>> GetAllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<NotificationChannel>>(channels);

    public Task SaveActivityAsync(string id, bool failed, DateTime at, CancellationToken cancellationToken = default)
    {
        ActivityErrors.Add(failed);
        return Task.CompletedTask;
    }

    public Task<NotificationChannel?> FindAsync(string id, CancellationToken cancellationToken = default) => throw new NotSupportedException();

    public Task<string> CreateAsync(string type, string displayName, string data, CancellationToken cancellationToken = default) => throw new NotSupportedException();

    public Task<bool> UpdateAsync(string id, string? displayName, string data, CancellationToken cancellationToken = default) => throw new NotSupportedException();

    public Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
}
