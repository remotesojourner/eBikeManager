namespace EBikeManager.IntegrationTests.Fixtures;

public sealed class PasswordProtectedApp : TestApp
{
    public const string Password = "correct horse battery";

    protected override async Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        SampleData.AddBikesAndRides(Bosch);
        await SampleData.CompleteSetupAsync(services, cancellationToken);
        await SampleData.SyncAsync(services, cancellationToken);
        await SampleData.RequirePasswordAsync(services, Password, cancellationToken);
    }
}
