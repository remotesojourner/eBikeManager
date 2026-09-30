using EBikeManager.Application.Models;

namespace EBikeManager.UnitTests.Application.Models;

public class OAuthCallbackTests
{
    [Fact]
    public void ReadsCodeAndStateFromTheRedirectUrl()
    {
        var callback = OAuthCallback.Parse(
            "  onebikeapp-ios://com.bosch.ebike.onebikeapp/oauth2redirect?state=st4te&session_state=abc-123&code=c0de.1-2%2F3  ");

        Assert.Equal(new OAuthCallback("c0de.1-2/3", "st4te", null), callback);
    }

    [Fact]
    public void ReadsErrorsReportedByTheLoginServer()
    {
        var callback = OAuthCallback.Parse("https://example.com/cb?error=access_denied&error_description=User+cancelled+login&state=s");

        Assert.Equal("User cancelled login", callback.Error);
        Assert.Null(callback.Code);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("just some text")]
    public void AnythingElseHasNoCode(string? pasted)
    {
        Assert.Equal(new OAuthCallback(null, null, null), OAuthCallback.Parse(pasted));
    }
}
