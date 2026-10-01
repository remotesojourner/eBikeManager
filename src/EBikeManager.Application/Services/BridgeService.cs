using EBikeManager.Application.Configuration;
using EBikeManager.Application.Models;
using EBikeManager.Application.Resources;
using EBikeManager.Application.Services.Interfaces;

namespace EBikeManager.Application.Services;

public sealed class BridgeService
{
    private readonly SettingsService _settings;
    private readonly SecretStoreService _secrets;
    private readonly IAppEventService _events;
    private readonly ICurrentAccessService _access;

    public BridgeService(SettingsService settings, SecretStoreService secrets, IAppEventService events, ICurrentAccessService access)
    {
        _settings = settings;
        _secrets = secrets;
        _events = events;
        _access = access;
    }

    public async Task<bool> HasEncryptionKeyAsync(CancellationToken cancellationToken = default) =>
        _access.HasFullAccess && await _secrets.GetAsync(SecretStoreService.BridgeEncryptionKey, cancellationToken) != null;

    public async Task<OperationResult> SaveAsync(BridgeRequest request, CancellationToken cancellationToken = default)
    {
        if (!_access.HasFullAccess) return OperationResult.Denied();

        var address = Clean(request.Address);
        if (address != null && !BridgeSettings.IsValidAddress(address)) return OperationResult.Invalid(ApplicationStrings.BridgeAddressInvalid);

        var key = Clean(request.EncryptionKey);
        if (key != null && !BridgeSettings.TryReadKey(key, out _)) return OperationResult.Invalid(ApplicationStrings.BridgeKeyInvalid);

        var saved = await _settings.SaveAsync(new Dictionary<string, string>
        {
            [SettingDefinitions.BridgeAddress] = address ?? SettingDefinitions.Unset,
            [SettingDefinitions.BridgeFirstBike] = Clean(request.FirstBikeId) ?? SettingDefinitions.Unset,
            [SettingDefinitions.BridgeSecondBike] = Clean(request.SecondBikeId) ?? SettingDefinitions.Unset
        }, cancellationToken);
        if (!saved.Succeeded) return saved;

        if (key != null) await _secrets.SetAsync(SecretStoreService.BridgeEncryptionKey, key, cancellationToken);
        else if (request.ClearEncryptionKey) await _secrets.DeleteAsync(SecretStoreService.BridgeEncryptionKey, cancellationToken);

        _events.PublishBridgeSettingsChanged();
        return OperationResult.Ok();
    }

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) || value.Trim() == SettingDefinitions.Unset ? null : value.Trim();
}
