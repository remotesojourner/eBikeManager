using EBikeManager.Application.Models;

namespace EBikeManager.Application.Services.Interfaces;

public interface IBoschAuthService
{
    string BuildAuthorizeUrl(PendingLogin login);

    Task<BoschTokens> ExchangeCodeAsync(string code, PendingLogin login, CancellationToken cancellationToken = default);

    Task<BoschTokens> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default);
}
