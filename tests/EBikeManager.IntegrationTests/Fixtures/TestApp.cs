using System.Reflection;
using EBikeManager.Application.Installers;
using EBikeManager.Application.Services.Interfaces;
using EBikeManager.TestSupport;
using EBikeManager.Web.Configuration;
using EBikeManager.Web.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace EBikeManager.IntegrationTests.Fixtures;

public abstract class TestApp : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "ebike-manager-tests", Guid.NewGuid().ToString("N"));

    internal FakeBoschApi Bosch { get; } = new();

    public string DataDirectory => Path.Combine(_root, "data");

    public async ValueTask InitializeAsync()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using (var scope = Services.CreateScope())
        {
            await SeedAsync(scope.ServiceProvider, cancellationToken);
        }

        await Services.GetRequiredService<AuthSettingsService>().ReloadAsync(cancellationToken);
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        SqliteConnection.ClearAllPools();
        if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
        GC.SuppressFinalize(this);
    }

    protected abstract Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken);

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting(EBikeManagerOptionsSetup.DataDirectoryVariable, DataDirectory);
        builder.UseSetting(EBikeManagerOptionsSetup.DisableAuthVariable, "false");
        builder.UseDefaultServiceProvider(options =>
        {
            options.ValidateOnBuild = true;
            options.ValidateScopes = true;
        });

        builder.ConfigureTestServices(services =>
        {
            RemoveTheAppsBackgroundServices(services);
            services.RemoveAll<IBoschApiService>();
            services.AddSingleton<IBoschApiService>(Bosch);
            services.RemoveAll<IBoschAuthService>();
            services.AddSingleton<IBoschAuthService, FakeBoschAuth>();
            services.ConfigureHttpClientDefaults(client => client.ConfigurePrimaryHttpMessageHandler(() => new NoNetworkHandler()));
        });
    }

    private static void RemoveTheAppsBackgroundServices(IServiceCollection services)
    {
        Assembly[] app = [typeof(Program).Assembly, typeof(ApplicationInstaller).Assembly];
        var backgroundServices = services
            .Where(service => service.ServiceType == typeof(IHostedService) && !service.IsKeyedService && app.Any(assembly => DeclaredIn(service, assembly)))
            .ToList();

        foreach (var service in backgroundServices) services.Remove(service);
    }

    private static bool DeclaredIn(ServiceDescriptor service, Assembly assembly) =>
        service.ImplementationType?.Assembly == assembly
        || service.ImplementationFactory?.Method.DeclaringType?.Assembly == assembly;

    private sealed class NoNetworkHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromException<HttpResponseMessage>(new HttpRequestException($"Tests have no network access, so {request.RequestUri} wasn't called"));
    }
}
