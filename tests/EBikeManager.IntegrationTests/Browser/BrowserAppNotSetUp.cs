using EBikeManager.IntegrationTests.Fixtures;

namespace EBikeManager.IntegrationTests.Browser;

public sealed class BrowserAppNotSetUp : BrowserApp
{
    protected override Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        SampleData.AddBikesAndRides(Bosch, rides: 6);
        return Task.CompletedTask;
    }
}
