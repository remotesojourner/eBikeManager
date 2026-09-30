using EBikeManager.Application.Configuration;
using EBikeManager.Application.Enums;
using EBikeManager.Application.Models;
using EBikeManager.Application.Repositories.Interfaces;
using EBikeManager.Application.Services;
using EBikeManager.Application.Services.Interfaces;
using EBikeManager.TestSupport;
using FakeItEasy;

namespace EBikeManager.UnitTests.Application.Services;

public class SignInServiceTests
{
    private const string Authority = "https://id.example.test/application/o/ebike-manager/";

    private readonly ISettingsRepository _settings = A.Fake<ISettingsRepository>();
    private readonly ISecretRepository _secrets = A.Fake<ISecretRepository>();
    private readonly IOidcDiscoveryService _discovery = A.Fake<IOidcDiscoveryService>();
    private readonly ISignInStateService _signInState = A.Fake<ISignInStateService>();
    private IReadOnlyDictionary<string, string> _saved = new Dictionary<string, string>();

    public SignInServiceTests()
    {
        A.CallTo(() => _settings.GetAsync(A<CancellationToken>._)).Returns(AppSettings.Defaults);
        A.CallTo(() => _settings.SaveSignInAsync(A<IReadOnlyDictionary<string, string>>._, A<CancellationToken>._))
            .ReturnsLazily((IReadOnlyDictionary<string, string> changes, CancellationToken _) =>
            {
                _saved = changes;
                return SettingsSaveResult.Saved;
            });
        A.CallTo(() => _discovery.FindProblemAsync(A<string>._, A<CancellationToken>._)).Returns((string?)null);
        A.CallTo(() => _signInState.IsActive).Returns(true);
    }

    [Fact]
    public async Task TurningSignInOnChecksTheProviderSavesItAndStartsANewStamp()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        var result = await Service(FixedAccess.Full).SaveAsync(new SignInRequest(true, $" {Authority} ", "ebike-manager", "secret", false, "profile email groups"), cancellationToken);

        Assert.True(result.Succeeded);
        Assert.True(result.Value);
        Assert.Equal("true", _saved[SettingDefinitions.AuthEnabled]);
        Assert.Equal(Authority, _saved[SettingDefinitions.OidcAuthority]);
        Assert.Equal("ebike-manager", _saved[SettingDefinitions.OidcClientId]);
        Assert.Equal("openid profile email groups", _saved[SettingDefinitions.OidcScopes]);
        Assert.NotEqual(SettingDefinitions.Unset, _saved[SettingDefinitions.AuthStamp]);
        A.CallTo(() => _discovery.FindProblemAsync(Authority, A<CancellationToken>._)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _secrets.SetAsync(SecretStoreService.OidcClientSecret, "protected:secret", A<DateTime>._, A<CancellationToken>._)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _signInState.ReloadAsync(A<CancellationToken>._)).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task AProviderThatFailsTheCheckIsNotSaved()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        A.CallTo(() => _discovery.FindProblemAsync(A<string>._, A<CancellationToken>._)).Returns("The provider answered 404.");

        var result = await Service(FixedAccess.Full).SaveAsync(new SignInRequest(true, Authority, "ebike-manager", null, false, null), cancellationToken);

        Assert.Equal((OperationOutcome.Invalid, "The provider answered 404."), (result.Outcome, result.Message));
        A.CallTo(() => _settings.SaveSignInAsync(A<IReadOnlyDictionary<string, string>>._, A<CancellationToken>._)).MustNotHaveHappened();
    }

    [Theory]
    [InlineData(null, "ebike-manager")]
    [InlineData(Authority, null)]
    [InlineData("ftp://id.example.test/", "ebike-manager")]
    public async Task SignInNeedsAProviderAddressAndAClientId(string? authority, string? clientId)
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        var result = await Service(FixedAccess.Full).SaveAsync(new SignInRequest(true, authority, clientId, null, false, null), cancellationToken);

        Assert.Equal(OperationOutcome.Invalid, result.Outcome);
        A.CallTo(() => _settings.SaveSignInAsync(A<IReadOnlyDictionary<string, string>>._, A<CancellationToken>._)).MustNotHaveHappened();
    }

    [Fact]
    public async Task OnlySomeoneWithFullAccessCanChangeSignIn()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        var result = await Service(FixedAccess.None).SaveAsync(new SignInRequest(false, null, null, null, false, null), cancellationToken);

        Assert.Equal(OperationOutcome.Denied, result.Outcome);
        Assert.False(await Service(FixedAccess.None).HasClientSecretAsync(cancellationToken));
    }

    [Fact]
    public async Task EveryoneStaysSignedInUnlessTheProviderChanges()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        A.CallTo(() => _settings.GetAsync(A<CancellationToken>._))
            .Returns(AppSettings.Defaults with { SignIn = new SignInSettings(true, Authority, "ebike-manager", "openid", "stamp") });

        await Service(FixedAccess.Full).SaveAsync(new SignInRequest(true, Authority, "ebike-manager", null, false, "openid groups"), cancellationToken);
        Assert.False(_saved.ContainsKey(SettingDefinitions.AuthStamp));

        await Service(FixedAccess.Full).SaveAsync(new SignInRequest(true, Authority, "other-client", null, false, null), cancellationToken);
        Assert.True(_saved.ContainsKey(SettingDefinitions.AuthStamp));
    }

    [Fact]
    public async Task TurningSignInOffNeedsNoProviderAndCanRemoveTheSecret()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        A.CallTo(() => _signInState.IsActive).Returns(false);

        var result = await Service(FixedAccess.Full).SaveAsync(new SignInRequest(false, "", "", null, true, ""), cancellationToken);

        Assert.True(result.Succeeded);
        Assert.False(result.Value);
        Assert.Equal(("false", SettingDefinitions.Unset, "openid"), (_saved[SettingDefinitions.AuthEnabled], _saved[SettingDefinitions.OidcAuthority], _saved[SettingDefinitions.OidcScopes]));
        A.CallTo(() => _discovery.FindProblemAsync(A<string>._, A<CancellationToken>._)).MustNotHaveHappened();
        A.CallTo(() => _secrets.DeleteAsync(SecretStoreService.OidcClientSecret, A<CancellationToken>._)).MustHaveHappenedOnceExactly();
    }

    private SignInService Service(ICurrentAccessService access) =>
        new(_settings, new SecretStoreService(_secrets, new PlainSecretProtection(), TimeProvider.System), _discovery, _signInState, access);
}
