using EBikeManager.Application.Enums;
using EBikeManager.Application.Exceptions;
using EBikeManager.Application.Models;
using EBikeManager.Application.Resources;
using EBikeManager.Application.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EBikeManager.Application.Services;

public sealed partial class BoschConnectionService : IDisposable
{
    private static readonly TimeSpan _expiryMargin = TimeSpan.FromMinutes(1);

    private readonly IServiceScopeFactory _scopes;
    private readonly IBoschAuthService _auth;
    private readonly TimeProvider _time;
    private readonly ILogger<BoschConnectionService> _logger;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private BoschTokens? _tokens;
    private bool _reauthRequired;

    public BoschConnectionService(IServiceScopeFactory scopes, IBoschAuthService auth, TimeProvider time, ILogger<BoschConnectionService> logger)
    {
        _scopes = scopes;
        _auth = auth;
        _time = time;
        _logger = logger;
    }

    public async Task<BoschConnectionStatus> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        if (await ReadRefreshTokenAsync(cancellationToken) == null) return BoschConnectionStatus.NotConnected;
        return _reauthRequired ? BoschConnectionStatus.ReauthRequired : BoschConnectionStatus.Connected;
    }

    public async Task SaveLoginAsync(BoschTokens tokens, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            await WriteRefreshTokenAsync(tokens.RefreshToken, cancellationToken);
            _tokens = tokens;
            _reauthRequired = false;
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

            var refreshToken = await ReadRefreshTokenAsync(cancellationToken)
                ?? throw new BoschReauthRequiredException(ApplicationStrings.BoschNotConnected);

            BoschTokens tokens;
            try
            {
                tokens = await _auth.RefreshAsync(refreshToken, cancellationToken);
            }
            catch (BoschReauthRequiredException ex)
            {
                LogLoginRejected(ex.Message);
                _tokens = null;
                _reauthRequired = true;
                throw;
            }

            if (tokens.RefreshToken != refreshToken) await WriteRefreshTokenAsync(tokens.RefreshToken, cancellationToken);
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

    private async Task<string?> ReadRefreshTokenAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopes.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<SecretStoreService>().GetAsync(SecretStoreService.BoschRefreshToken, cancellationToken);
    }

    private async Task WriteRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken)
    {
        using var scope = _scopes.CreateScope();
        await scope.ServiceProvider.GetRequiredService<SecretStoreService>().SetAsync(SecretStoreService.BoschRefreshToken, refreshToken, cancellationToken);
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "The Bosch login needs to be renewed: {Reason}")]
    private partial void LogLoginRejected(string reason);
}
