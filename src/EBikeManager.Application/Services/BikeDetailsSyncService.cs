using System.Text.Json;
using EBikeManager.Application.Models;
using EBikeManager.Application.Models.Entities;
using EBikeManager.Application.Repositories.Interfaces;
using EBikeManager.Application.Resources;
using EBikeManager.Application.Services.Interfaces;
using EBikeManager.Application.Utils;
using Microsoft.Extensions.Logging;

namespace EBikeManager.Application.Services;

public sealed partial class BikeDetailsSyncService
{
    private readonly IBoschApiService _bosch;
    private readonly IBikeRepository _bikes;
    private readonly IBikePictureRepository _pictures;
    private readonly TimeProvider _time;
    private readonly ILogger<BikeDetailsSyncService> _logger;

    public BikeDetailsSyncService(IBoschApiService bosch, IBikeRepository bikes, IBikePictureRepository pictures, TimeProvider time, ILogger<BikeDetailsSyncService> logger)
    {
        _bosch = bosch;
        _bikes = bikes;
        _pictures = pictures;
        _time = time;
        _logger = logger;
    }

    public async Task<IReadOnlyList<string>> RefreshAsync(CancellationToken cancellationToken = default)
    {
        var problems = new List<string>();
        var hasFlowPlus = await OptionalAsync(_bosch.HasFlowPlusAsync, cancellationToken);

        foreach (var bike in await _bikes.GetAllAsync(cancellationToken))
        {
            try
            {
                var profile = await _bosch.GetBikeProfileJsonAsync(bike.Id, cancellationToken);
                if (profile == null)
                {
                    problems.Add(ApplicationStrings.Format(ApplicationStrings.BikeProfileMissing, bike.Name));
                    continue;
                }

                var snapshot = new BikeSnapshot(
                    profile,
                    await OptionalAsync(token => _bosch.GetStateOfChargeJsonAsync(bike.Id, token), cancellationToken),
                    await OptionalAsync(token => _bosch.GetBikePassJsonAsync(bike.Id, token), cancellationToken),
                    await OptionalAsync(token => _bosch.GetLatestLocationJsonAsync(bike.Id, token), cancellationToken),
                    hasFlowPlus);
                await _bikes.SaveSnapshotAsync(bike.Id, snapshot, _time.GetUtcNow().UtcDateTime, cancellationToken);
                await RefreshPictureAsync(bike.Id, profile, cancellationToken);
            }
            catch (HttpRequestException ex)
            {
                LogRefreshFailed(ex, bike.Id);
                problems.Add(ApplicationStrings.Format(ApplicationStrings.BikeRefreshFailed, bike.Name, ex.Message));
            }
        }

        return problems;
    }

    private async Task RefreshPictureAsync(string bikeId, string profileJson, CancellationToken cancellationToken)
    {
        if (PictureAddress(profileJson) is not { } address) return;
        if (await _pictures.GetSourceUrlAsync(bikeId, cancellationToken) == address.AbsoluteUri) return;

        try
        {
            if (await _bosch.DownloadBikePictureAsync(address, cancellationToken) is not { } content) return;
            if (ImageFormat.ContentTypeOf(content) is not { } contentType)
            {
                LogPictureNotAnImage(bikeId);
                return;
            }

            await _pictures.SaveAsync(
                new BikePicture { BikeId = bikeId, SourceUrl = address.AbsoluteUri, ContentType = contentType, Content = content, SavedAt = _time.GetUtcNow().UtcDateTime },
                cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            LogPictureFailed(ex, bikeId);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            LogPictureFailed(ex, bikeId);
        }
    }

    private static Uri? PictureAddress(string profileJson)
    {
        using var profile = JsonDocument.Parse(profileJson);
        return BoschJson.PictureUrl(profile.RootElement);
    }

    private async Task<T?> OptionalAsync<T>(Func<CancellationToken, Task<T?>> call, CancellationToken cancellationToken)
    {
        try
        {
            return await call(cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            LogOptionalDetailFailed(ex);
            return default;
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Could not refresh the details of bike {BikeId}")]
    private partial void LogRefreshFailed(Exception exception, string bikeId);

    [LoggerMessage(Level = LogLevel.Information, Message = "An optional bike detail couldn't be read from Bosch")]
    private partial void LogOptionalDetailFailed(Exception exception);

    [LoggerMessage(Level = LogLevel.Information, Message = "Could not download the picture of bike {BikeId}")]
    private partial void LogPictureFailed(Exception exception, string bikeId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "The picture Bosch gave for bike {BikeId} isn't a PNG, JPEG, GIF or WebP image, so it wasn't saved")]
    private partial void LogPictureNotAnImage(string bikeId);
}
