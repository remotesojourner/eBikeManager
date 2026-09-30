using EBikeManager.Application.Models;
using EBikeManager.Application.Services;
using Microsoft.Extensions.Time.Testing;

namespace EBikeManager.UnitTests.Application.Services;

public class SyncStateServiceTests
{
    [Fact]
    public void OnlyOneSyncRunsAtATimeAndEveryChangeIsPublished()
    {
        var events = new AppEventService();
        var published = new List<SyncStatus>();
        events.SyncStatusChanged += published.Add;
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 9, 30, 12, 0, 0, TimeSpan.Zero));
        var state = new SyncStateService(events, time);

        Assert.True(state.TryStart("Starting"));
        Assert.False(state.TryStart("Starting"));
        state.ReportProgress("Page 1");
        var result = new SyncRunResult(3, 1, 1, []);
        state.Finish(result, null);

        Assert.Equal(["Starting", "Page 1", null], published.Select(status => status.Progress));
        Assert.Equal(new SyncStatus(false, null, time.GetUtcNow().UtcDateTime, result, null), state.Status);
        Assert.True(state.TryStart("Again"));
    }

    [Fact]
    public void AFailedSyncKeepsTheLastResult()
    {
        var state = new SyncStateService(new AppEventService(), TimeProvider.System);
        var result = new SyncRunResult(3, 1, 1, []);
        state.TryStart("Starting");
        state.Finish(result, null);

        state.TryStart("Starting");
        state.Finish(null, "Bosch is down");

        Assert.Same(result, state.Status.LastResult);
        Assert.Equal("Bosch is down", state.Status.LastError);
    }
}
