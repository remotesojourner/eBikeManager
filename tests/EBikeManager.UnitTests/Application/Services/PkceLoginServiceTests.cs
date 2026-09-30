using EBikeManager.Application.Services;
using Microsoft.Extensions.Time.Testing;

namespace EBikeManager.UnitTests.Application.Services;

public class PkceLoginServiceTests
{
    [Fact]
    public void ChallengeMatchesTheRfc7636Example()
    {
        Assert.Equal("E9Melhoa2OwvFrEMTJguCHaoeK1t8URWbuGJSstw-cM", PkceLoginService.CreateChallenge("dBjftJeZ4CVP-mB92K27uhbUJU1p1r_wW1gFWFOEjXk"));
    }

    [Fact]
    public void ALoginCanBeTakenOnceByItsProvider()
    {
        var service = new PkceLoginService(new FakeTimeProvider());
        var login = service.Start("bosch");

        Assert.Equal(PkceLoginService.CreateChallenge(login.CodeVerifier), login.CodeChallenge);
        Assert.Same(login, service.Take("bosch", login.State));
        Assert.Null(service.Take("bosch", login.State));
    }

    [Fact]
    public void LoginsOfAnotherProviderOrPastTheirLifetimeAreRefused()
    {
        var time = new FakeTimeProvider();
        var service = new PkceLoginService(time);

        Assert.Null(service.Take("google", service.Start("bosch").State));

        var expired = service.Start("bosch");
        time.Advance(PkceLoginService.Lifetime + TimeSpan.FromSeconds(1));
        Assert.Null(service.Take("bosch", expired.State));
    }
}
