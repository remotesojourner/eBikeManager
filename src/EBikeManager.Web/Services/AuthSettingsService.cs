using EBikeManager.Application.Configuration;
using EBikeManager.Application.Services;
using EBikeManager.Application.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace EBikeManager.Web.Services;

public sealed class AuthSettingsService : ISignInStateService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly bool _disabledByEnvironment;
    private SignInSettings _signIn = AppSettings.Defaults.SignIn;

    public AuthSettingsService(IServiceScopeFactory scopes, IOptions<EBikeManagerOptions> options)
    {
        _scopes = scopes;
        _disabledByEnvironment = options.Value.DisableAuth;
    }

    public bool DisabledByEnvironment => _disabledByEnvironment;

    public bool IsActive => !_disabledByEnvironment && _signIn.IsActive;

    public string? Stamp => _signIn.Stamp;

    public async Task ReloadAsync(CancellationToken cancellationToken = default)
    {
        using var scope = _scopes.CreateScope();
        _signIn = (await scope.ServiceProvider.GetRequiredService<SettingsService>().GetAsync(cancellationToken)).SignIn;
    }
}
