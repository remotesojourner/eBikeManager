using EBikeManager.Application.Repositories.Interfaces;
using EBikeManager.Application.Services.Interfaces;

namespace EBikeManager.Application.Services;

public sealed class SecretStoreService
{
    public const string BoschRefreshToken = "boschRefreshToken";
    public const string GoogleHealthRefreshToken = "googleHealthRefreshToken";
    public const string GoogleHealthClientSecret = "googleHealthClientSecret";
    public const string OidcClientSecret = "oidcClientSecret";

    private readonly ISecretRepository _secrets;
    private readonly ISecretProtectionService _protection;
    private readonly TimeProvider _time;

    public SecretStoreService(ISecretRepository secrets, ISecretProtectionService protection, TimeProvider time)
    {
        _secrets = secrets;
        _protection = protection;
        _time = time;
    }

    public async Task<string?> GetAsync(string name, CancellationToken cancellationToken = default) =>
        await _secrets.GetAsync(name, cancellationToken) is { } stored ? _protection.Unprotect(stored) : null;

    public Task SetAsync(string name, string value, CancellationToken cancellationToken = default) =>
        _secrets.SetAsync(name, _protection.Protect(value), _time.GetUtcNow().UtcDateTime, cancellationToken);

    public Task DeleteAsync(string name, CancellationToken cancellationToken = default) => _secrets.DeleteAsync(name, cancellationToken);
}
