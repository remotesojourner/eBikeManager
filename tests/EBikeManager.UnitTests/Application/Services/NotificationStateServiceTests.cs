using EBikeManager.Application.Services;

namespace EBikeManager.UnitTests.Application.Services;

public class NotificationStateServiceTests
{
    [Fact]
    public void OnlyTheFirstFailureAndTheFirstSuccessAfterItAreReported()
    {
        var state = new NotificationStateService();

        Assert.False(state.SyncWorked());
        Assert.True(state.SyncFailed());
        Assert.False(state.SyncFailed());
        Assert.True(state.SyncWorked());
        Assert.False(state.SyncWorked());
        Assert.True(state.SyncFailed());
    }

    [Fact]
    public void ASignInIsReportedOnceUntilTheServiceIsSignedInAgain()
    {
        var state = new NotificationStateService();

        Assert.True(state.SignInNeeded("Google Health"));
        Assert.False(state.SignInNeeded("Google Health"));
        Assert.True(state.SignInNeeded("Bosch eBike Flow"));

        state.SignedIn("Google Health");

        Assert.True(state.SignInNeeded("Google Health"));
        Assert.False(state.SignInNeeded("Bosch eBike Flow"));
    }
}
