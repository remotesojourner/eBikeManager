using EBikeManager.Application.Configuration;
using EBikeManager.Application.Enums;
using EBikeManager.Application.Models;
using EBikeManager.Application.Repositories.Interfaces;
using EBikeManager.Application.Services;
using EBikeManager.Application.Services.Interfaces;
using EBikeManager.Application.Utils;
using EBikeManager.TestSupport;
using FakeItEasy;

namespace EBikeManager.UnitTests.Application.Services;

public class SignInServiceTests
{
    private readonly ISettingsRepository _settings = A.Fake<ISettingsRepository>();
    private readonly ISignInStateService _signInState = A.Fake<ISignInStateService>();
    private IReadOnlyDictionary<string, string> _saved = new Dictionary<string, string>();

    public SignInServiceTests()
    {
        A.CallTo(() => _settings.SaveSignInAsync(A<IReadOnlyDictionary<string, string>>._, A<CancellationToken>._))
            .ReturnsLazily((IReadOnlyDictionary<string, string> changes, CancellationToken _) =>
            {
                _saved = changes;
                return SettingsSaveResult.Saved;
            });
    }

    [Fact]
    public async Task TurningOnStoresAHashAndANewStampThenReloadsTheSignInState()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        var result = await new SignInService(_settings, _signInState, FixedAccess.Full).TurnOnAsync("long enough", cancellationToken);

        Assert.True(result.Succeeded);
        Assert.Equal("true", _saved[SettingDefinitions.AuthEnabled]);
        Assert.True(PasswordHash.Verify("long enough", _saved[SettingDefinitions.AuthPasswordHash]));
        Assert.NotEqual(SettingDefinitions.Unset, _saved[SettingDefinitions.AuthStamp]);
        A.CallTo(() => _signInState.ReloadAsync(A<CancellationToken>._)).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task ShortPasswordsAndVisitorsWithoutAccessAreRefused()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        Assert.Equal(OperationOutcome.Invalid, (await new SignInService(_settings, _signInState, FixedAccess.Full).TurnOnAsync("short", cancellationToken)).Outcome);
        Assert.Equal(OperationOutcome.Denied, (await new SignInService(_settings, _signInState, FixedAccess.None).TurnOnAsync("long enough", cancellationToken)).Outcome);
        A.CallTo(() => _settings.SaveSignInAsync(A<IReadOnlyDictionary<string, string>>._, A<CancellationToken>._)).MustNotHaveHappened();
    }

    [Fact]
    public async Task ThePasswordIsCheckedAgainstTheStoredHash()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        A.CallTo(() => _settings.GetAsync(A<CancellationToken>._))
            .Returns(AppSettings.Defaults with { SignIn = new SignInSettings(true, PasswordHash.Create("long enough"), "stamp") });
        var service = new SignInService(_settings, _signInState, FixedAccess.None);

        Assert.True((await service.CheckPasswordAsync("long enough", cancellationToken)).Succeeded);
        Assert.Equal(OperationOutcome.Invalid, (await service.CheckPasswordAsync("wrong", cancellationToken)).Outcome);
    }
}
