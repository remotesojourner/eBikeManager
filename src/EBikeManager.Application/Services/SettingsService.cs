using EBikeManager.Application.Configuration;
using EBikeManager.Application.Models;
using EBikeManager.Application.Repositories.Interfaces;
using EBikeManager.Application.Services.Interfaces;

namespace EBikeManager.Application.Services;

public sealed class SettingsService
{
    private readonly ISettingsRepository _settings;
    private readonly IAppEventService _events;
    private readonly ICurrentAccessService _access;

    public SettingsService(ISettingsRepository settings, IAppEventService events, ICurrentAccessService access)
    {
        _settings = settings;
        _events = events;
        _access = access;
    }

    public Task<AppSettings> GetAsync(CancellationToken cancellationToken = default) => _settings.GetAsync(cancellationToken);

    public async Task<OperationResult> SaveAsync(IReadOnlyDictionary<string, string> changes, CancellationToken cancellationToken = default)
    {
        if (!_access.HasFullAccess) return OperationResult.Denied();

        var saved = await _settings.SaveAsync(changes, cancellationToken);
        if (!saved.Succeeded) return OperationResult.Invalid(saved.Error!);

        _events.PublishSettingsChanged(changes);
        return OperationResult.Ok();
    }
}
