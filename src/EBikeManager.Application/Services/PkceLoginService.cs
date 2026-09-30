using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using EBikeManager.Application.Models;

namespace EBikeManager.Application.Services;

public sealed class PkceLoginService
{
    public static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(30);

    private readonly ConcurrentDictionary<string, PendingLogin> _logins = new();
    private readonly TimeProvider _time;

    public PkceLoginService(TimeProvider time)
    {
        _time = time;
    }

    public PendingLogin Start(string provider)
    {
        var now = _time.GetUtcNow().UtcDateTime;
        foreach (var (state, _) in _logins.Where(login => login.Value.ExpiresAt <= now)) _logins.TryRemove(state, out _);

        var verifier = Base64Url(RandomNumberGenerator.GetBytes(32));
        var login = new PendingLogin(provider, Base64Url(RandomNumberGenerator.GetBytes(16)), verifier, CreateChallenge(verifier), now + Lifetime);
        _logins[login.State] = login;
        return login;
    }

    public PendingLogin? Take(string provider, string state) =>
        _logins.TryRemove(state, out var login) && login.Provider == provider && login.ExpiresAt > _time.GetUtcNow().UtcDateTime
            ? login
            : null;

    public static string CreateChallenge(string verifier) => Base64Url(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)));

    private static string Base64Url(byte[] bytes) => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
