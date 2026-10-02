using System.Collections.Concurrent;

namespace EBikeManager.Application.Services;

internal sealed class HeartbeatScheduleService
{
    private static readonly TimeSpan _earlyTolerance = TimeSpan.FromSeconds(30);

    private readonly ConcurrentDictionary<string, DateTimeOffset> _lastSent = new();
    private readonly TimeProvider _time;

    public HeartbeatScheduleService(TimeProvider time)
    {
        _time = time;
    }

    public bool IsDue(string channelId, int intervalMinutes)
    {
        if (intervalMinutes <= 1) return true;

        var now = _time.GetUtcNow();
        if (_lastSent.TryGetValue(channelId, out var last) && now - last < TimeSpan.FromMinutes(intervalMinutes) - _earlyTolerance) return false;

        _lastSent[channelId] = now;
        return true;
    }
}
