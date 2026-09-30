using System.Security.Claims;
using EBikeManager.Application.Enums;
using EBikeManager.Web.Utils;

namespace EBikeManager.UnitTests.Web.Utils;

public class AccessPolicyTests
{
    [Fact]
    public void WithoutAPasswordEveryoneHasFullAccess()
    {
        Assert.Equal(Access.Full, AccessPolicy.Decide(signInActive: false, user: null, stamp: null));
    }

    [Fact]
    public void WithAPasswordOnlyACookieFromTheCurrentPasswordGetsIn()
    {
        Assert.Equal(Access.Full, AccessPolicy.Decide(true, SignInCookie.CreatePrincipal("stamp-2"), "stamp-2"));
        Assert.Equal(Access.None, AccessPolicy.Decide(true, SignInCookie.CreatePrincipal("stamp-1"), "stamp-2"));
        Assert.Equal(Access.None, AccessPolicy.Decide(true, new ClaimsPrincipal(new ClaimsIdentity()), "stamp-2"));
        Assert.Equal(Access.None, AccessPolicy.Decide(true, null, "stamp-2"));
    }

    [Theory]
    [InlineData("/settings", "/settings")]
    [InlineData("/rides?page=2", "/rides?page=2")]
    [InlineData(null, "/")]
    [InlineData("https://evil.example", "/")]
    [InlineData("//evil.example", "/")]
    [InlineData("/\\evil.example", "/")]
    public void OnlyLocalReturnUrlsAreKept(string? returnUrl, string expected)
    {
        Assert.Equal(expected, AccessPolicy.LocalReturnUrl(returnUrl));
    }
}
