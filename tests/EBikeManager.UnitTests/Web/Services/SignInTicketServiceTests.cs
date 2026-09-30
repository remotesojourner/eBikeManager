using EBikeManager.Web.Services;
using Microsoft.Extensions.Time.Testing;

namespace EBikeManager.UnitTests.Web.Services;

public class SignInTicketServiceTests
{
    [Fact]
    public void TicketsWorkOnceAndOnlyForAMinute()
    {
        var time = new FakeTimeProvider();
        var tickets = new SignInTicketService(time);

        var used = tickets.Issue();
        Assert.True(tickets.TryRedeem(used));
        Assert.False(tickets.TryRedeem(used));

        var late = tickets.Issue();
        time.Advance(TimeSpan.FromMinutes(1));
        Assert.False(tickets.TryRedeem(late));
        Assert.False(tickets.TryRedeem("made-up"));
    }
}
