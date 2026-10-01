using EBikeManager.Application.Configuration;
using EBikeManager.Application.Models.Dtos;
using EBikeManager.Application.Repositories.Interfaces;
using EBikeManager.Application.Services;
using EBikeManager.IntegrationTests.Fixtures;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;

namespace EBikeManager.IntegrationTests.Browser;

public abstract class BrowserApp : TestApp
{
    protected BrowserApp()
    {
        UseKestrel(0);
    }

    public Uri BaseAddress => new(Services.GetRequiredService<IServer>().Features.GetRequiredFeature<IServerAddressesFeature>().Addresses.First());

    public override async ValueTask InitializeAsync()
    {
        await base.InitializeAsync();
        Oidc.AuthorizeEndpoint = new Uri(BaseAddress, FakeOidcProvider.AuthorizePath);
    }

    public async Task<AppSettings> SettingsAsync(CancellationToken cancellationToken)
    {
        using var scope = Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<ISettingsRepository>().GetAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<BikeDto>> BikesAsync(CancellationToken cancellationToken)
    {
        using var scope = Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<BikeService>().GetBikesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<RideDto>> RidesAsync(CancellationToken cancellationToken)
    {
        using var scope = Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<RideService>().GetRidesAsync(cancellationToken: cancellationToken);
    }
}
