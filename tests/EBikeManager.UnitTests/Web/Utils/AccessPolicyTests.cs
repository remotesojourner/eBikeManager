using System.Security.Claims;
using EBikeManager.Application.Enums;
using EBikeManager.Web.Utils;
using Microsoft.AspNetCore.Http;

namespace EBikeManager.UnitTests.Web.Utils;

public class AccessPolicyTests
{
    [Fact]
    public void WithoutSignInEveryoneHasFullAccess()
    {
        Assert.Equal(Access.Full, AccessPolicy.Decide(signInActive: false, user: null, stamp: null));
    }

    [Fact]
    public void WithSignInOnlyASignInFromTheCurrentProviderGetsIn()
    {
        Assert.Equal(Access.Full, AccessPolicy.Decide(true, SignedIn("stamp-2"), "stamp-2"));
        Assert.Equal(Access.None, AccessPolicy.Decide(true, SignedIn("stamp-1"), "stamp-2"));
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

    [Fact]
    public void TheSignInAddressCarriesTheLocalPageToReturnTo()
    {
        Assert.Equal("/auth/login?returnUrl=%2Frides%3Fpage%3D2", AccessPolicy.SignInPathFor("/rides?page=2"));
        Assert.Equal("/auth/login?returnUrl=%2F", AccessPolicy.SignInPathFor("https://evil.example"));
    }

    [Theory]
    [InlineData("/_blazor/negotiate", true)]
    [InlineData("/bikes/bike-1/picture", true)]
    [InlineData("/bikes", false)]
    [InlineData("/rides", false)]
    [InlineData("/", false)]
    public void TheHubAndMediaAnswerWithAStatusWhilePagesGoToSignIn(string path, bool expected)
    {
        Assert.Equal(expected, AccessPolicy.AnswersWithStatus(new PathString(path)));
    }

    private static ClaimsPrincipal SignedIn(string stamp) =>
        new(new ClaimsIdentity([new Claim("sub", "rider-1"), new Claim(SignInCookie.StampClaim, stamp)], "Cookies"));
}
