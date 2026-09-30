using EBikeManager.Application.Exceptions;
using EBikeManager.Application.Models;
using EBikeManager.Application.Services.Interfaces;

namespace EBikeManager.TestSupport;

internal sealed class FakeBoschAuth : IBoschAuthService
{
    public const string ValidCode = "valid-code";
    public const string Account = "rider@example.com";

    public string BuildAuthorizeUrl(PendingLogin login) => $"https://localhost/bosch-login?state={login.State}";

    public Task<BoschTokens> ExchangeCodeAsync(string code, PendingLogin login, CancellationToken cancellationToken = default) =>
        code == ValidCode
            ? Task.FromResult(new BoschTokens("access", "refresh", DateTime.UtcNow.AddMinutes(5), Account))
            : Task.FromException<BoschTokens>(new BoschReauthRequiredException("invalid_grant"));

    public Task<BoschTokens> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default) =>
        Task.FromResult(new BoschTokens("access", refreshToken, DateTime.UtcNow.AddMinutes(5), Account));

    public static string RedirectFor(string authorizeUrl, string code = ValidCode) =>
        $"onebikeapp-ios://com.bosch.ebike.onebikeapp/oauth2redirect?state={new Uri(authorizeUrl).Query.Split("state=")[1]}&session_state=s&code={code}";
}
