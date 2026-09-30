using EBikeManager.Application.Configuration;
using EBikeManager.Application.Exceptions;
using EBikeManager.Application.Models;
using EBikeManager.Application.Models.Dtos;
using EBikeManager.Application.Models.Entities;
using EBikeManager.Application.Repositories.Interfaces;
using EBikeManager.Application.Resources;
using EBikeManager.Application.Services.Interfaces;
using EBikeManager.Application.Utils;

namespace EBikeManager.Application.Services;

public sealed class BikeService
{
    private const int RecentRidesForModeNames = 20;

    private readonly IBikeRepository _bikes;
    private readonly IBikePictureRepository _pictures;
    private readonly IBikeDocumentRepository _documents;
    private readonly IRideRepository _rides;
    private readonly BikeDetailsSyncService _detailsSync;
    private readonly SettingsService _settings;
    private readonly ICurrentAccessService _access;
    private readonly TimeProvider _time;

    public BikeService(IBikeRepository bikes, IBikePictureRepository pictures, IBikeDocumentRepository documents, IRideRepository rides, BikeDetailsSyncService detailsSync, SettingsService settings, ICurrentAccessService access, TimeProvider time)
    {
        _bikes = bikes;
        _pictures = pictures;
        _documents = documents;
        _rides = rides;
        _detailsSync = detailsSync;
        _settings = settings;
        _access = access;
        _time = time;
    }

    public async Task<IReadOnlyList<BikeDto>> GetBikesAsync(CancellationToken cancellationToken = default) =>
        (await _bikes.GetAllAsync(cancellationToken)).Select(BikeDto.From).ToList();

    public async Task<IReadOnlyList<BikeDetailsDto>> GetDetailsAsync(CancellationToken cancellationToken = default)
    {
        var details = new List<BikeDetailsDto>();
        var pictures = await _pictures.GetSavedTimesAsync(cancellationToken);
        var documents = (await _documents.GetInfoAsync(cancellationToken)).ToLookup(document => document.BikeId);
        foreach (var bike in await _bikes.GetAllAsync(cancellationToken))
        {
            var modeNames = BoschJson.AssistModeNames(await _rides.GetRecentSummariesAsync(bike.Id, RecentRidesForModeNames, cancellationToken));
            details.Add(BikeProfileParser.Parse(
                bike,
                modeNames,
                pictures.TryGetValue(bike.Id, out var savedAt) ? savedAt : null,
                [.. documents[bike.Id].Select(BikeDocumentDto.From)]));
        }

        return details;
    }

    public async Task<OperationResult<MediaFile>> GetPictureAsync(string bikeId, CancellationToken cancellationToken = default)
    {
        if (!_access.HasFullAccess) return OperationResult.Denied();
        if (await _pictures.FindAsync(bikeId, cancellationToken) is not { } picture) return OperationResult.NotFound(ApplicationStrings.BikePictureMissing);

        return OperationResult.Ok(new MediaFile(picture.ContentType, picture.Content));
    }

    public async Task<OperationResult<MediaFile>> GetDocumentAsync(string bikeId, string fileId, CancellationToken cancellationToken = default)
    {
        if (!_access.HasFullAccess) return OperationResult.Denied();
        if (await _documents.FindAsync(bikeId, fileId, cancellationToken) is not { } document) return OperationResult.NotFound(ApplicationStrings.BikeDocumentMissing);

        return OperationResult.Ok(new MediaFile(document.ContentType, document.Content));
    }

    public async Task<OperationResult<IReadOnlyList<string>>> RefreshDetailsAsync(CancellationToken cancellationToken = default)
    {
        if (!_access.HasFullAccess) return OperationResult.Denied();

        try
        {
            return OperationResult.Ok(await _detailsSync.RefreshAsync(cancellationToken));
        }
        catch (BoschReauthRequiredException)
        {
            return OperationResult.Invalid(ApplicationStrings.BoschReconnectNeeded);
        }
    }

    public async Task<OperationResult> SaveSelectionAsync(IReadOnlyList<BoschBikeInfo> chosen, CancellationToken cancellationToken = default)
    {
        if (!_access.HasFullAccess) return OperationResult.Denied();
        if (chosen.Count == 0) return OperationResult.Invalid(ApplicationStrings.BikesNoneChosen);

        var before = (await _bikes.GetAllAsync(cancellationToken)).Select(bike => bike.Id).ToHashSet();
        var now = _time.GetUtcNow().UtcDateTime;
        await _bikes.ReplaceAsync(chosen.Select(bike => new Bike { Id = bike.Id, Name = bike.Name, AddedAt = now }).ToList(), cancellationToken);

        if (before.SetEquals(chosen.Select(bike => bike.Id))) return OperationResult.Ok();
        return await _settings.SaveAsync(new Dictionary<string, string> { [SettingDefinitions.BoschFullScan] = "true" }, cancellationToken);
    }
}
