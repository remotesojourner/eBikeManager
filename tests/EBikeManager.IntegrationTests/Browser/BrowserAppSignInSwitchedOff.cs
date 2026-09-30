using EBikeManager.IntegrationTests.Fixtures;

namespace EBikeManager.IntegrationTests.Browser;

public sealed class BrowserAppSignInSwitchedOff : BrowserApp
{
    protected override bool SignInSwitchedOffByEnvironment => true;

    protected override async Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        SampleData.AddBikesAndRides(Bosch);
        await SampleData.CompleteSetupAsync(services, cancellationToken);
        await SampleData.SyncAsync(services, cancellationToken);
        await SampleData.RequireSignInAsync(services, cancellationToken);
    }
}
