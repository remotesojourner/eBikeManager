using EBikeManager.Application.Data;
using EBikeManager.Application.Models;
using EBikeManager.Application.Models.Entities;
using EBikeManager.Application.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EBikeManager.Application.Repositories;

internal sealed class BikeDocumentRepository : IBikeDocumentRepository
{
    private readonly EBikeManagerDbContext _db;

    public BikeDocumentRepository(EBikeManagerDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<BikeDocumentInfo>> GetInfoAsync(CancellationToken cancellationToken = default)
    {
        var documents = await _db.BikeDocuments.AsNoTracking()
            .Select(document => new BikeDocumentInfo(document.BikeId, document.FileId, document.FileType, document.ContentType, document.AddedAt, document.SourceUpdatedAt, document.SavedAt))
            .ToListAsync(cancellationToken);
        return [.. documents.OrderBy(document => document.AddedAt ?? document.SavedAt).ThenBy(document => document.FileId, StringComparer.Ordinal)];
    }

    public Task<BikeDocument?> FindAsync(string bikeId, string fileId, CancellationToken cancellationToken = default) =>
        _db.BikeDocuments.AsNoTracking().SingleOrDefaultAsync(document => document.BikeId == bikeId && document.FileId == fileId, cancellationToken);

    public async Task SaveAsync(BikeDocument document, CancellationToken cancellationToken = default)
    {
        if (!await _db.Bikes.AnyAsync(bike => bike.Id == document.BikeId, cancellationToken)) return;

        if (await _db.BikeDocuments.SingleOrDefaultAsync(existing => existing.BikeId == document.BikeId && existing.FileId == document.FileId, cancellationToken) is { } stored)
        {
            stored.FileType = document.FileType;
            stored.ContentType = document.ContentType;
            stored.Content = document.Content;
            stored.AddedAt = document.AddedAt;
            stored.SourceUpdatedAt = document.SourceUpdatedAt;
            stored.SavedAt = document.SavedAt;
        }
        else
        {
            _db.BikeDocuments.Add(document);
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    public Task KeepOnlyAsync(string bikeId, IReadOnlyCollection<string> fileIds, CancellationToken cancellationToken = default) =>
        _db.BikeDocuments
            .Where(document => document.BikeId == bikeId && !fileIds.Contains(document.FileId))
            .ExecuteDeleteAsync(cancellationToken);
}
