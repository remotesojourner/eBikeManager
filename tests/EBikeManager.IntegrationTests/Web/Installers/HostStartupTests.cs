using EBikeManager.Application.Services;
using EBikeManager.Application.Services.Interfaces;
using EBikeManager.IntegrationTests.Fixtures;
using Microsoft.Extensions.DependencyInjection;

namespace EBikeManager.IntegrationTests.Web.Installers;

public sealed class HostStartupTests : IClassFixture<NotSetUpApp>
{
    private readonly NotSetUpApp _app;

    public HostStartupTests(NotSetUpApp app)
    {
        _app = app;
    }

    [Fact]
    public async Task TheAppStartsWithItsDatabaseInTheDataDirectory()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var client = _app.CreateClient();

        var page = await client.GetStringAsync(new Uri("/", UriKind.Relative), cancellationToken);

        Assert.Contains("<title>eBike Manager</title>", page, StringComparison.Ordinal);
        Assert.True(File.Exists(Path.Combine(_app.DataDirectory, "storage.db")));
    }

    [Fact]
    public async Task TheHealthEndpointAnswersWithoutSigningIn()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var client = _app.CreateClient();

        Assert.Equal("Healthy", await client.GetStringAsync(new Uri(EBikeManager.Web.Utils.HealthEndpoint.Path, UriKind.Relative), cancellationToken));
    }

    [Fact]
    public async Task SecretsAreEncryptedWithTheAppsKeys()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var scope = _app.Services.CreateScope();
        var secrets = scope.ServiceProvider.GetRequiredService<SecretStoreService>();

        await secrets.SetAsync("test", "secret value", cancellationToken);

        Assert.Equal("secret value", await secrets.GetAsync("test", cancellationToken));
        Assert.NotEqual("secret value", scope.ServiceProvider.GetRequiredService<ISecretProtectionService>().Protect("secret value"));
        Assert.NotEmpty(Directory.GetFiles(Path.Combine(_app.DataDirectory, "keys")));
    }
}
