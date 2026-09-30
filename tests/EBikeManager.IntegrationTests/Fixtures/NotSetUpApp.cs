namespace EBikeManager.IntegrationTests.Fixtures;

public sealed class NotSetUpApp : TestApp
{
    protected override Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        SampleData.AddBikesAndRides(Bosch);
        return Task.CompletedTask;
    }
}
