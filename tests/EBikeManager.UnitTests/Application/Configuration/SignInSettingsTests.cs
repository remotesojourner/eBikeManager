using EBikeManager.Application.Configuration;

namespace EBikeManager.UnitTests.Application.Configuration;

public class SignInSettingsTests
{
    [Theory]
    [InlineData(null, "openid")]
    [InlineData("", "openid")]
    [InlineData("profile email", "openid profile email")]
    [InlineData("openid, groups  groups profile", "openid groups profile")]
    public void OpenIdIsAlwaysTheFirstScopeAndEachScopeCountsOnce(string? scopes, string expected)
    {
        Assert.Equal(expected, string.Join(' ', SignInSettings.ParseScopes(scopes)));
    }

    [Theory]
    [InlineData("https://auth.example.com/application/o/ebike-manager/", true)]
    [InlineData("http://keycloak.local:8080/realms/home", true)]
    [InlineData("ftp://auth.example.com/", false)]
    [InlineData("https://auth.example.com/?client=1", false)]
    [InlineData("auth.example.com", false)]
    [InlineData(null, false)]
    public void TheProviderIsAFullWebAddress(string? authority, bool expected)
    {
        Assert.Equal(expected, SignInSettings.IsValidAuthority(authority));
    }

    [Fact]
    public void SignInIsOnlyActiveOnceTurnedOnWithAProviderAndClient()
    {
        Assert.True(new SignInSettings(true, "https://auth.example.com/", "ebike", "openid", "stamp").IsActive);
        Assert.False(new SignInSettings(false, "https://auth.example.com/", "ebike", "openid", "stamp").IsActive);
        Assert.False(new SignInSettings(true, null, "ebike", "openid", "stamp").IsActive);
        Assert.False(new SignInSettings(true, "https://auth.example.com/", null, "openid", "stamp").IsActive);
    }

    [Fact]
    public void ByDefaultSignInIsOffAndAsksForTheUsualScopes()
    {
        var signIn = AppSettings.Defaults.SignIn;

        Assert.False(signIn.IsActive);
        Assert.Equal(["openid", "profile", "email"], signIn.ScopeList);
    }
}
