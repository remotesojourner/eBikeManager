using System.Collections.Concurrent;
using System.Security.Cryptography;

namespace EBikeManager.Web.Services;

public sealed class SignInTicketService
{
    private static readonly TimeSpan _lifetime = TimeSpan.FromMinutes(1);

    private readonly ConcurrentDictionary<string, DateTime> _tickets = new();
    private readonly TimeProvider _time;

    public SignInTicketService(TimeProvider time)
    {
        _time = time;
    }

    public string Issue()
    {
        var now = _time.GetUtcNow().UtcDateTime;
        foreach (var (expired, _) in _tickets.Where(ticket => ticket.Value <= now)) _tickets.TryRemove(expired, out _);

        var ticket = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        _tickets[ticket] = now + _lifetime;
        return ticket;
    }

    public bool TryRedeem(string ticket) =>
        _tickets.TryRemove(ticket, out var expiresAt) && expiresAt > _time.GetUtcNow().UtcDateTime;
}
