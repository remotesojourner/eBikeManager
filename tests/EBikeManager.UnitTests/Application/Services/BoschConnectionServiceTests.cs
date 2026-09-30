using EBikeManager.Application.Enums;
using EBikeManager.Application.Exceptions;
using EBikeManager.Application.Models;
using EBikeManager.Application.Repositories.Interfaces;
using EBikeManager.Application.Services;
using EBikeManager.Application.Services.Interfaces;
using EBikeManager.TestSupport;
using FakeItEasy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;

namespace EBikeManager.UnitTests.Application.Services;

public sealed class BoschConnectionServiceTests : IDisposable
{
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 9, 30, 12, 0, 0, TimeSpan.Zero));
    private readonly Dictionary<string, string> _secrets = [];
    private readonly IBoschAuthService _auth = A.Fake<IBoschAuthService>();
    private readonly ServiceProvider _services;
    private readonly BoschConnectionService _connection;

    public BoschConnectionServiceTests()
    {
        var repository = A.Fake<ISecretRepository>();
        A.CallTo(() => repository.GetAsync(A<string>._, A<CancellationToken>._)).ReturnsLazily((string name, CancellationToken _) => _secrets.GetValueOrDefault(name));
        A.CallTo(() => repository.SetAsync(A<string>._, A<string>._, A<DateTime>._, A<CancellationToken>._))
            .Invokes((string name, string value, DateTime _, CancellationToken _) => _secrets[name] = value);

        _services = new ServiceCollection()
            .AddScoped(_ => repository)
            .AddSingleton<ISecretProtectionService, PlainSecretProtection>()
            .AddSingleton<TimeProvider>(_time)
            .AddScoped<SecretStoreService>()
            .BuildServiceProvider();
        _connection = new BoschConnectionService(_services.GetRequiredService<IServiceScopeFactory>(), _auth, _time, NullLogger<BoschConnectionService>.Instance);
    }

    public void Dispose()
    {
        _connection.Dispose();
        _services.Dispose();
    }

    [Fact]
    public async Task ASavedLoginIsEncryptedAndReusedUntilItsAccessTokenExpires()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        A.CallTo(() => _auth.RefreshAsync("refresh-1", A<CancellationToken>._))
            .Returns(new BoschTokens("access-2", "refresh-2", _time.GetUtcNow().UtcDateTime.AddMinutes(10), null));

        await _connection.SaveLoginAsync(new BoschTokens("access-1", "refresh-1", _time.GetUtcNow().UtcDateTime.AddMinutes(5), "rider"), cancellationToken);

        Assert.Equal("protected:refresh-1", _secrets[SecretStoreService.BoschRefreshToken]);
        Assert.Equal(BoschConnectionStatus.Connected, await _connection.GetStatusAsync(cancellationToken));
        Assert.Equal("access-1", await _connection.GetAccessTokenAsync(cancellationToken: cancellationToken));
        A.CallTo(() => _auth.RefreshAsync(A<string>._, A<CancellationToken>._)).MustNotHaveHappened();

        _time.Advance(TimeSpan.FromMinutes(5));

        Assert.Equal("access-2", await _connection.GetAccessTokenAsync(cancellationToken: cancellationToken));
        Assert.Equal("protected:refresh-2", _secrets[SecretStoreService.BoschRefreshToken]);
    }

    [Fact]
    public async Task ARejectedRefreshTokenAsksForANewLogin()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        _secrets[SecretStoreService.BoschRefreshToken] = "protected:stale";
        A.CallTo(() => _auth.RefreshAsync("stale", A<CancellationToken>._)).ThrowsAsync(new BoschReauthRequiredException("invalid_grant"));

        await Assert.ThrowsAsync<BoschReauthRequiredException>(() => _connection.GetAccessTokenAsync(cancellationToken: cancellationToken));

        Assert.Equal(BoschConnectionStatus.ReauthRequired, await _connection.GetStatusAsync(cancellationToken));
    }

    [Fact]
    public async Task WithoutASavedLoginThereIsNothingToRefresh()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        Assert.Equal(BoschConnectionStatus.NotConnected, await _connection.GetStatusAsync(cancellationToken));
        await Assert.ThrowsAsync<BoschReauthRequiredException>(() => _connection.GetAccessTokenAsync(cancellationToken: cancellationToken));
    }
}
