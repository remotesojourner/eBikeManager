using EBikeManager.Application.Models;

namespace EBikeManager.Application.Services.Interfaces;

public interface IGoogleHealthAuthService
{
    string BuildAuthorizeUrl(PendingLogin login, string clientId);

    Task<GoogleHealthTokens> ExchangeCodeAsync(string code, PendingLogin login, GoogleHealthClient client, CancellationToken cancellationToken = default);

    Task<GoogleHealthTokens> RefreshAsync(string refreshToken, GoogleHealthClient client, CancellationToken cancellationToken = default);

    Task RevokeAsync(string token, CancellationToken cancellationToken = default);
}
