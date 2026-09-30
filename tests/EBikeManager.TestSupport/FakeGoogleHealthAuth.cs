using EBikeManager.Application.Exceptions;
using EBikeManager.Application.Models;
using EBikeManager.Application.Services.Interfaces;

namespace EBikeManager.TestSupport;

internal sealed class FakeGoogleHealthAuth : IGoogleHealthAuthService
{
    public const string ValidCode = "google-code";
    public const string Account = "rider@gmail.com";
    public const string ClientId = "123456789012-test.apps.googleusercontent.com";
    public const string ClientSecret = "test-secret";

    public List<string> Revoked { get; } = [];

    public string BuildAuthorizeUrl(PendingLogin login, string clientId) => $"{login.RedirectUri}?state={login.State}&code={ValidCode}&scope=email";

    public Task<GoogleHealthTokens> ExchangeCodeAsync(string code, PendingLogin login, GoogleHealthClient client, CancellationToken cancellationToken = default) =>
        code == ValidCode && client.ClientSecret == ClientSecret
            ? Task.FromResult(new GoogleHealthTokens("access", "google-refresh", DateTime.UtcNow.AddHours(1), Account))
            : Task.FromException<GoogleHealthTokens>(new IntegrationSignInRequiredException("Google didn't accept the code."));

    public Task<GoogleHealthTokens> RefreshAsync(string refreshToken, GoogleHealthClient client, CancellationToken cancellationToken = default) =>
        Task.FromResult(new GoogleHealthTokens("access", refreshToken, DateTime.UtcNow.AddHours(1), null));

    public Task RevokeAsync(string token, CancellationToken cancellationToken = default)
    {
        Revoked.Add(token);
        return Task.CompletedTask;
    }
}
