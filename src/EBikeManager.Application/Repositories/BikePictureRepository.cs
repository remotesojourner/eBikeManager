using EBikeManager.Application.Data;
using EBikeManager.Application.Models.Entities;
using EBikeManager.Application.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EBikeManager.Application.Repositories;

internal sealed class BikePictureRepository : IBikePictureRepository
{
    private readonly EBikeManagerDbContext _db;

    public BikePictureRepository(EBikeManagerDbContext db)
    {
        _db = db;
    }

    public Task<string?> GetSourceUrlAsync(string bikeId, CancellationToken cancellationToken = default) =>
        _db.BikePictures.AsNoTracking()
            .Where(picture => picture.BikeId == bikeId)
            .Select(picture => (string?)picture.SourceUrl)
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyDictionary<string, DateTime>> GetSavedTimesAsync(CancellationToken cancellationToken = default) =>
        await _db.BikePictures.AsNoTracking()
            .Select(picture => new { picture.BikeId, picture.SavedAt })
            .ToDictionaryAsync(picture => picture.BikeId, picture => picture.SavedAt, cancellationToken);

    public Task<BikePicture?> FindAsync(string bikeId, CancellationToken cancellationToken = default) =>
        _db.BikePictures.AsNoTracking().SingleOrDefaultAsync(picture => picture.BikeId == bikeId, cancellationToken);

    public async Task SaveAsync(BikePicture picture, CancellationToken cancellationToken = default)
    {
        if (!await _db.Bikes.AnyAsync(bike => bike.Id == picture.BikeId, cancellationToken)) return;

        if (await _db.BikePictures.SingleOrDefaultAsync(existing => existing.BikeId == picture.BikeId, cancellationToken) is { } stored)
        {
            stored.SourceUrl = picture.SourceUrl;
            stored.ContentType = picture.ContentType;
            stored.Content = picture.Content;
            stored.SavedAt = picture.SavedAt;
        }
        else
        {
            _db.BikePictures.Add(picture);
        }

        await _db.SaveChangesAsync(cancellationToken);
    }
}
