using EBikeManager.Application.Enums;
using EBikeManager.Application.Exceptions;
using EBikeManager.Application.Models;
using EBikeManager.Application.Repositories.Interfaces;
using EBikeManager.Application.Resources;
using EBikeManager.Application.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EBikeManager.Application.Services;

public sealed partial class GoogleHealthConnectionService : IDisposable
{
    private static readonly TimeSpan _expiryMargin = TimeSpan.FromMinutes(1);

    private readonly IServiceScopeFactory _scopes;
    private readonly IGoogleHealthAuthService _auth;
    private readonly TimeProvider _time;
    private readonly ILogger<GoogleHealthConnectionService> _logger;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private GoogleHealthTokens? _tokens;
    private bool _reauthRequired;
    private string? _signInProblem;

    public GoogleHealthConnectionService(IServiceScopeFactory scopes, IGoogleHealthAuthService auth, TimeProvider time, ILogger<GoogleHealthConnectionService> logger)
    {
        _scopes = scopes;
        _auth = auth;
        _time = time;
        _logger = logger;
    }

    public async Task<GoogleHealthConnectionStatus> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        if (await ReadSecretAsync(SecretStoreService.GoogleHealthRefreshToken, cancellationToken) == null) return GoogleHealthConnectionStatus.NotConnected;
        return _reauthRequired ? GoogleHealthConnectionStatus.ReauthRequired : GoogleHealthConnectionStatus.Connected;
    }

    public async Task<GoogleHealthClient?> GetClientAsync(CancellationToken cancellationToken = default)
    {
        using var scope = _scopes.CreateScope();
        var clientId = (await scope.ServiceProvider.GetRequiredService<ISettingsRepository>().GetAsync(cancellationToken)).GoogleHealth.ClientId;
        var secret = await scope.ServiceProvider.GetRequiredService<SecretStoreService>().GetAsync(SecretStoreService.GoogleHealthClientSecret, cancellationToken);
        return clientId != null && secret != null ? new GoogleHealthClient(clientId, secret) : null;
    }

    public Task SaveClientSecretAsync(string clientSecret, CancellationToken cancellationToken = default) =>
        WriteSecretAsync(SecretStoreService.GoogleHealthClientSecret, clientSecret, cancellationToken);

    public void RecordSignInProblem(string? problem) => _signInProblem = problem;

    public string? TakeSignInProblem() => Interlocked.Exchange(ref _signInProblem, null);

    public async Task SaveLoginAsync(GoogleHealthTokens tokens, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            await WriteSecretAsync(SecretStoreService.GoogleHealthRefreshToken, tokens.RefreshToken, cancellationToken);
            _tokens = tokens;
            _reauthRequired = false;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<string?> ForgetAsync(CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            var refreshToken = await ReadSecretAsync(SecretStoreService.GoogleHealthRefreshToken, cancellationToken);
            using var scope = _scopes.CreateScope();
            await scope.ServiceProvider.GetRequiredService<SecretStoreService>().DeleteAsync(SecretStoreService.GoogleHealthRefreshToken, cancellationToken);
            _tokens = null;
            _reauthRequired = false;
            return refreshToken;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<string> GetAccessTokenAsync(bool forceRefresh = false, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (!forceRefresh && _tokens is { } cached && cached.AccessTokenExpiresAt - _expiryMargin > _time.GetUtcNow().UtcDateTime) return cached.AccessToken;

            var refreshToken = await ReadSecretAsync(SecretStoreService.GoogleHealthRefreshToken, cancellationToken)
                ?? throw new IntegrationSignInRequiredException(ApplicationStrings.GoogleHealthNotConnected);
            var client = await GetClientAsync(cancellationToken)
                ?? throw new IntegrationSignInRequiredException(ApplicationStrings.GoogleHealthNotConnected);

            GoogleHealthTokens tokens;
            try
            {
                tokens = await _auth.RefreshAsync(refreshToken, client, cancellationToken);
            }
            catch (IntegrationSignInRequiredException ex)
            {
                LogLoginRejected(ex.Message);
                _tokens = null;
                _reauthRequired = true;
                throw;
            }

            if (tokens.RefreshToken != refreshToken) await WriteSecretAsync(SecretStoreService.GoogleHealthRefreshToken, tokens.RefreshToken, cancellationToken);
            _tokens = tokens;
            _reauthRequired = false;
            return tokens.AccessToken;
        }
        finally
        {
            _lock.Release();
        }
    }

    public void Dispose() => _lock.Dispose();

    private async Task<string?> ReadSecretAsync(string name, CancellationToken cancellationToken)
    {
        using var scope = _scopes.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<SecretStoreService>().GetAsync(name, cancellationToken);
    }

    private async Task WriteSecretAsync(string name, string value, CancellationToken cancellationToken)
    {
        using var scope = _scopes.CreateScope();
        await scope.ServiceProvider.GetRequiredService<SecretStoreService>().SetAsync(name, value, cancellationToken);
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "The Google Health login needs to be renewed: {Reason}")]
    private partial void LogLoginRejected(string reason);
}
