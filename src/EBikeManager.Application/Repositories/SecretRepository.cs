using EBikeManager.Application.Data;
using EBikeManager.Application.Models.Entities;
using EBikeManager.Application.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EBikeManager.Application.Repositories;

internal sealed class SecretRepository : ISecretRepository
{
    private readonly EBikeManagerDbContext _db;

    public SecretRepository(EBikeManagerDbContext db)
    {
        _db = db;
    }

    public async Task<string?> GetAsync(string name, CancellationToken cancellationToken = default) =>
        await _db.Secrets.AsNoTracking().Where(secret => secret.Name == name).Select(secret => secret.Value).SingleOrDefaultAsync(cancellationToken);

    public async Task SetAsync(string name, string value, DateTime updatedAt, CancellationToken cancellationToken = default)
    {
        var secret = await _db.Secrets.SingleOrDefaultAsync(entry => entry.Name == name, cancellationToken);
        if (secret == null)
        {
            secret = new SecretEntry { Name = name };
            _db.Secrets.Add(secret);
        }

        secret.Value = value;
        secret.UpdatedAt = updatedAt;
        await _db.SaveChangesAsync(cancellationToken);
    }

    public Task DeleteAsync(string name, CancellationToken cancellationToken = default) =>
        _db.Secrets.Where(secret => secret.Name == name).ExecuteDeleteAsync(cancellationToken);
}
