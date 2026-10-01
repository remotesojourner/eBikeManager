using EBikeManager.Application.Configuration;
using EBikeManager.Application.Models;
using EBikeManager.Application.Repositories.Interfaces;
using EBikeManager.Application.Resources;
using EBikeManager.Application.Services.Interfaces;
using EBikeManager.Application.Utils;

namespace EBikeManager.Application.Services;

public sealed class SignInService
{
    private readonly ISettingsRepository _settings;
    private readonly SecretStoreService _secrets;
    private readonly IOidcDiscoveryService _discovery;
    private readonly ISignInStateService _signInState;
    private readonly ICurrentAccessService _access;

    public SignInService(ISettingsRepository settings, SecretStoreService secrets, IOidcDiscoveryService discovery, ISignInStateService signInState, ICurrentAccessService access)
    {
        _settings = settings;
        _secrets = secrets;
        _discovery = discovery;
        _signInState = signInState;
        _access = access;
    }

    public async Task<bool> HasClientSecretAsync(CancellationToken cancellationToken = default) =>
        _access.HasFullAccess && await _secrets.GetAsync(SecretStoreService.OidcClientSecret, cancellationToken) != null;

    public async Task<OperationResult<bool>> SaveAsync(SignInRequest request, CancellationToken cancellationToken = default)
    {
        if (!_access.HasFullAccess) return OperationResult.Denied();

        var authority = Clean(request.Authority);
        if (authority != null && !SignInSettings.IsValidAuthority(authority)) return OperationResult.Invalid(ApplicationStrings.SignInProviderUrlInvalid);

        var clientId = Clean(request.ClientId);
        if (request.Enabled)
        {
            if (authority == null || clientId == null) return OperationResult.Invalid(ApplicationStrings.SignInIncomplete);
            if (await _discovery.FindProblemAsync(authority, cancellationToken) is { } problem) return OperationResult.Invalid(problem);
        }

        var current = (await _settings.GetAsync(cancellationToken)).SignIn;
        var changes = new Dictionary<string, string>
        {
            [SettingDefinitions.AuthEnabled] = request.Enabled ? "true" : "false",
            [SettingDefinitions.OidcAuthority] = authority ?? SettingDefinitions.Unset,
            [SettingDefinitions.OidcClientId] = clientId ?? SettingDefinitions.Unset,
            [SettingDefinitions.OidcScopes] = string.Join(' ', SignInSettings.ParseScopes(request.Scopes))
        };
        if (request.Enabled && (!current.IsActive || authority != current.Authority || clientId != current.ClientId))
            changes[SettingDefinitions.AuthStamp] = Guid.NewGuid().ToString("N");

        var saved = await _settings.SaveSignInAsync(changes, cancellationToken);
        if (!saved.Succeeded) return OperationResult.Invalid(saved.Error!);

        if (Clean(request.ClientSecret) is { } secret) await _secrets.SetAsync(SecretStoreService.OidcClientSecret, secret, cancellationToken);
        else if (request.ClearClientSecret) await _secrets.DeleteAsync(SecretStoreService.OidcClientSecret, cancellationToken);

        await _signInState.ReloadAsync(cancellationToken);
        return OperationResult.Ok(_signInState.IsActive);
    }

    public async Task<OperationResult<string>> GetApiTokenAsync(CancellationToken cancellationToken = default)
    {
        if (!_access.HasFullAccess) return OperationResult.Denied();
        if (!_signInState.IsActive) return OperationResult.Invalid(ApplicationStrings.ApiTokenNeedsSignIn);

        return OperationResult.Ok(await _secrets.GetAsync(SecretStoreService.ApiTokenSecret, cancellationToken) ?? await CreateApiTokenAsync(cancellationToken));
    }

    public async Task<OperationResult<string>> RegenerateApiTokenAsync(CancellationToken cancellationToken = default)
    {
        if (!_access.HasFullAccess) return OperationResult.Denied();
        if (!_signInState.IsActive) return OperationResult.Invalid(ApplicationStrings.ApiTokenNeedsSignIn);

        return OperationResult.Ok(await CreateApiTokenAsync(cancellationToken));
    }

    private async Task<string> CreateApiTokenAsync(CancellationToken cancellationToken)
    {
        var token = ApiToken.Generate();
        await _secrets.SetAsync(SecretStoreService.ApiTokenSecret, token, cancellationToken);
        await _signInState.ReloadAsync(cancellationToken);
        return token;
    }

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) || value.Trim() == SettingDefinitions.Unset ? null : value.Trim();
}
