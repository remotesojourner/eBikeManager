using EBikeManager.IntegrationTests.Fixtures;

namespace EBikeManager.IntegrationTests.Browser;

public sealed class BrowserAppWithoutRides : BrowserApp
{
    protected override Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken) =>
        SampleData.CompleteSetupAsync(services, cancellationToken);
}
