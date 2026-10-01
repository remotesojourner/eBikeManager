using EBikeManager.Application.Models.Entities;
using EBikeManager.Application.Repositories.Interfaces;
using EBikeManager.IntegrationTests.Fixtures;
using Microsoft.Extensions.DependencyInjection;

namespace EBikeManager.IntegrationTests.Browser;

public sealed class BrowserAppWithTwoBikes : BrowserApp
{
    public const string OtherBikeName = "Cube (Performance Line CX)";
    public const string OtherBikeRide = "Cube commute";

    protected override async Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        SampleData.AddBikesAndRides(Bosch);
        Bosch.AddRide("cube-00", SampleData.OtherBikeId, DateTime.UtcNow.AddDays(-1), title: OtherBikeRide);
        await SampleData.CompleteSetupAsync(services, cancellationToken);
        await services.GetRequiredService<IBikeRepository>().ReplaceAsync(
        [
            new Bike { Id = SampleData.BikeId, Name = "TENWAYS (Performance Line)", Model = "TENWAYS (Performance Line)", AddedAt = DateTime.UtcNow },
            new Bike { Id = SampleData.OtherBikeId, Name = OtherBikeName, Model = OtherBikeName, AddedAt = DateTime.UtcNow }
        ], cancellationToken);
        await SampleData.SyncAsync(services, cancellationToken);
    }
}
