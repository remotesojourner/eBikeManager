namespace EBikeManager.IntegrationTests.Fixtures;

public sealed class SignInRequiredApp : TestApp
{
    protected override async Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        SampleData.AddBikesAndRides(Bosch);
        await SampleData.CompleteSetupAsync(services, cancellationToken);
        await SampleData.SyncAsync(services, cancellationToken);
        await SampleData.RequireSignInAsync(services, cancellationToken);
    }
}
