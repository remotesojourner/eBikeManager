using EBikeManager.Application.Configuration;
using EBikeManager.Application.Services;
using EBikeManager.Application.Services.Interfaces;
using EBikeManager.Application.Utils;
using EBikeManager.Web.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Options;

namespace EBikeManager.Web.Services;

public sealed class AuthSettingsService : ISignInStateService, IDisposable
{
    public const string OidcScheme = OpenIdConnectDefaults.AuthenticationScheme;

    private readonly IServiceScopeFactory _scopes;
    private readonly IAuthenticationSchemeProvider _schemes;
    private readonly IOptionsMonitorCache<OpenIdConnectOptions> _oidcOptions;
    private readonly bool _disabledByEnvironment;
    private readonly SemaphoreSlim _reloadLock = new(1, 1);

    public AuthSettingsService(IServiceScopeFactory scopes, IAuthenticationSchemeProvider schemes, IOptionsMonitorCache<OpenIdConnectOptions> oidcOptions, IOptions<EBikeManagerOptions> options)
    {
        _scopes = scopes;
        _schemes = schemes;
        _oidcOptions = oidcOptions;
        _disabledByEnvironment = options.Value.DisableAuth;
        Current = new AuthSnapshot(AppSettings.Defaults.SignIn, null, null, _disabledByEnvironment);
    }

    public AuthSnapshot Current { get; private set; }

    public bool DisabledByEnvironment => _disabledByEnvironment;

    public bool IsActive => Current.IsActive;

    public string? Stamp => Current.SignIn.Stamp;

    public async Task ReloadAsync(CancellationToken cancellationToken = default)
    {
        await _reloadLock.WaitAsync(cancellationToken);
        try
        {
            using var scope = _scopes.CreateScope();
            var signIn = (await scope.ServiceProvider.GetRequiredService<SettingsService>().GetAsync(cancellationToken)).SignIn;
            var secrets = scope.ServiceProvider.GetRequiredService<SecretStoreService>();
            var clientSecret = await secrets.GetAsync(SecretStoreService.OidcClientSecret, cancellationToken);
            var apiToken = await secrets.GetAsync(SecretStoreService.ApiTokenSecret, cancellationToken);
            Current = new AuthSnapshot(signIn, clientSecret, apiToken is { } token ? ApiToken.Hash(token) : null, _disabledByEnvironment);

            _oidcOptions.TryRemove(OidcScheme);
            var registered = await _schemes.GetSchemeAsync(OidcScheme) != null;
            if (Current.IsActive && !registered) _schemes.TryAddScheme(new AuthenticationScheme(OidcScheme, OpenIdConnectDefaults.DisplayName, typeof(OpenIdConnectHandler)));
            else if (!Current.IsActive && registered) _schemes.RemoveScheme(OidcScheme);
        }
        finally
        {
            _reloadLock.Release();
        }
    }

    public void Dispose() => _reloadLock.Dispose();
}
