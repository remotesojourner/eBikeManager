using EBikeManager.Application.Configuration;
using EBikeManager.Application.Models;
using EBikeManager.Application.Models.Dtos;
using EBikeManager.Application.Models.Entities;
using EBikeManager.Application.Repositories.Interfaces;
using EBikeManager.Application.Resources;
using EBikeManager.Application.Services.Interfaces;

namespace EBikeManager.Application.Services;

public sealed class BikeService
{
    private readonly IBikeRepository _bikes;
    private readonly SettingsService _settings;
    private readonly ICurrentAccessService _access;
    private readonly TimeProvider _time;

    public BikeService(IBikeRepository bikes, SettingsService settings, ICurrentAccessService access, TimeProvider time)
    {
        _bikes = bikes;
        _settings = settings;
        _access = access;
        _time = time;
    }

    public async Task<IReadOnlyList<BikeDto>> GetBikesAsync(CancellationToken cancellationToken = default) =>
        (await _bikes.GetAllAsync(cancellationToken)).Select(BikeDto.From).ToList();

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
