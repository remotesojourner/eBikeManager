namespace EBikeManager.Application.Repositories.Interfaces;

public interface ISecretRepository
{
    Task<string?> GetAsync(string name, CancellationToken cancellationToken = default);
    Task SetAsync(string name, string value, DateTime updatedAt, CancellationToken cancellationToken = default);
    Task DeleteAsync(string name, CancellationToken cancellationToken = default);
}
