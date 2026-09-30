using EBikeManager.Application.Configuration;
using EBikeManager.Application.Enums;
using EBikeManager.Application.Models;
using EBikeManager.Application.Repositories.Interfaces;
using EBikeManager.Application.Services;
using EBikeManager.Application.Services.Interfaces;
using EBikeManager.TestSupport;
using FakeItEasy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace EBikeManager.UnitTests.Application.Services;

public sealed class BoschAccountServiceTests : IDisposable
{
    private readonly Dictionary<string, string> _secrets = [];
    private readonly ISettingsRepository _settings = A.Fake<ISettingsRepository>();
    private readonly ServiceProvider _services;

    public BoschAccountServiceTests()
    {
        var secrets = A.Fake<ISecretRepository>();
        A.CallTo(() => secrets.GetAsync(A<string>._, A<CancellationToken>._)).ReturnsLazily((string name, CancellationToken _) => _secrets.GetValueOrDefault(name));
        A.CallTo(() => secrets.SetAsync(A<string>._, A<string>._, A<DateTime>._, A<CancellationToken>._))
            .Invokes((string name, string value, DateTime _, CancellationToken _) => _secrets[name] = value);
        A.CallTo(() => _settings.SaveAsync(A<IReadOnlyDictionary<string, string>>._, A<CancellationToken>._)).Returns(SettingsSaveResult.Saved);

        _services = new ServiceCollection()
            .AddLogging()
            .AddSingleton(TimeProvider.System)
            .AddSingleton<IAppEventService, AppEventService>()
            .AddSingleton<PkceLoginService>()
            .AddSingleton<IBoschAuthService, FakeBoschAuth>()
            .AddSingleton<BoschConnectionService>()
            .AddSingleton<IBoschApiService>(new FakeBoschApi())
            .AddSingleton<ISecretProtectionService, PlainSecretProtection>()
            .AddSingleton<ICurrentAccessService>(FixedAccess.Full)
            .AddScoped(_ => secrets)
            .AddScoped(_ => _settings)
            .AddScoped<SecretStoreService>()
            .AddScoped<SettingsService>()
            .AddScoped<BoschAccountService>()
            .BuildServiceProvider();
    }

    public void Dispose() => _services.Dispose();

    [Fact]
    public async Task PastingTheRedirectUrlConnectsTheAccount()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var service = _services.GetRequiredService<BoschAccountService>();

        var result = await service.ConnectAsync(FakeBoschAuth.RedirectFor(service.StartLogin().Value!), cancellationToken);

        Assert.True(result.Succeeded, result.Message);
        Assert.Equal(BoschConnectionStatus.Connected, await service.GetStatusAsync(cancellationToken));
        A.CallTo(() => _settings.SaveAsync(
                A<IReadOnlyDictionary<string, string>>.That.Matches(changes => changes[SettingDefinitions.BoschAccount] == FakeBoschAuth.Account),
                A<CancellationToken>._))
            .MustHaveHappened();
    }

    [Theory]
    [InlineData("hello", "doesn't look like the oauth2redirect URL")]
    [InlineData("https://x/cb?state=unknown&code=abc", "older login attempt")]
    [InlineData("https://x/cb?error=access_denied&error_description=Cancelled", "Bosch reported an error: Cancelled")]
    public async Task PastesThatCannotBeUsedSayWhy(string pasted, string reason)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var service = _services.GetRequiredService<BoschAccountService>();

        var result = await service.ConnectAsync(pasted, cancellationToken);

        Assert.Equal(OperationOutcome.Invalid, result.Outcome);
        Assert.Contains(reason, result.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AnExpiredCodeIsExplained()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var service = _services.GetRequiredService<BoschAccountService>();

        var result = await service.ConnectAsync(FakeBoschAuth.RedirectFor(service.StartLogin().Value!, "expired"), cancellationToken);

        Assert.Contains("probably expired", result.Message, StringComparison.Ordinal);
        Assert.Empty(_secrets);
    }

    [Fact]
    public async Task VisitorsWithoutAccessCannotConnect()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var service = new BoschAccountService(
            _services.GetRequiredService<PkceLoginService>(),
            new FakeBoschAuth(),
            _services.GetRequiredService<BoschConnectionService>(),
            new FakeBoschApi(),
            _services.CreateScope().ServiceProvider.GetRequiredService<SettingsService>(),
            FixedAccess.None,
            NullLogger<BoschAccountService>.Instance);

        Assert.Equal(OperationOutcome.Denied, service.StartLogin().Outcome);
        Assert.Equal(OperationOutcome.Denied, (await service.ConnectAsync("anything", cancellationToken)).Outcome);
    }
}
