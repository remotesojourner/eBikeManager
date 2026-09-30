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
    private readonly ISignInStateService _signInState;
    private readonly ICurrentAccessService _access;

    public SignInService(ISettingsRepository settings, ISignInStateService signInState, ICurrentAccessService access)
    {
        _settings = settings;
        _signInState = signInState;
        _access = access;
    }

    public async Task<OperationResult> TurnOnAsync(string password, CancellationToken cancellationToken = default)
    {
        if (!_access.HasFullAccess) return OperationResult.Denied();
        if (password.Length < PasswordHash.MinimumLength) return OperationResult.Invalid(ApplicationStrings.Format(ApplicationStrings.PasswordTooShort, PasswordHash.MinimumLength));

        var hash = await Task.Run(() => PasswordHash.Create(password), cancellationToken);
        return await SaveAsync(new Dictionary<string, string>
        {
            [SettingDefinitions.AuthEnabled] = "true",
            [SettingDefinitions.AuthPasswordHash] = hash,
            [SettingDefinitions.AuthStamp] = Guid.NewGuid().ToString("N")
        }, cancellationToken);
    }

    public async Task<OperationResult> TurnOffAsync(CancellationToken cancellationToken = default)
    {
        if (!_access.HasFullAccess) return OperationResult.Denied();

        return await SaveAsync(new Dictionary<string, string>
        {
            [SettingDefinitions.AuthEnabled] = "false",
            [SettingDefinitions.AuthPasswordHash] = SettingDefinitions.Unset,
            [SettingDefinitions.AuthStamp] = SettingDefinitions.Unset
        }, cancellationToken);
    }

    public async Task<OperationResult> CheckPasswordAsync(string password, CancellationToken cancellationToken = default)
    {
        var signIn = (await _settings.GetAsync(cancellationToken)).SignIn;
        if (!signIn.IsActive) return OperationResult.Ok();

        var matches = await Task.Run(() => PasswordHash.Verify(password, signIn.PasswordHash!), cancellationToken);
        return matches ? OperationResult.Ok() : OperationResult.Invalid(ApplicationStrings.PasswordWrong);
    }

    private async Task<OperationResult> SaveAsync(Dictionary<string, string> changes, CancellationToken cancellationToken)
    {
        var saved = await _settings.SaveSignInAsync(changes, cancellationToken);
        if (!saved.Succeeded) return OperationResult.Invalid(saved.Error!);

        await _signInState.ReloadAsync(cancellationToken);
        return OperationResult.Ok();
    }
}
