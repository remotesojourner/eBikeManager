using EBikeManager.IntegrationTests.Fixtures;

namespace EBikeManager.IntegrationTests.Browser;

public sealed class BrowserAppWithRides : BrowserApp
{
    protected override async Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        SampleData.AddBikesAndRides(Bosch);
        await SampleData.CompleteSetupAsync(services, cancellationToken);
        await SampleData.SyncAsync(services, cancellationToken);
    }
}
