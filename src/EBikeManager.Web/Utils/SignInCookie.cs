using System.Security.Claims;
using EBikeManager.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace EBikeManager.Web.Utils;

public static class SignInCookie
{
    public const string Name = "EBikeManager.Auth";
    public const string StampClaim = "passwordStamp";

    public static ClaimsPrincipal CreatePrincipal(string? stamp) =>
        new(new ClaimsIdentity([new Claim(ClaimTypes.Name, "owner"), new Claim(StampClaim, stamp ?? "")], CookieAuthenticationDefaults.AuthenticationScheme));

    public static async Task ValidateAsync(CookieValidatePrincipalContext context)
    {
        var auth = context.HttpContext.RequestServices.GetRequiredService<AuthSettingsService>();
        if (AccessPolicy.StampMatches(auth.IsActive, context.Principal, auth.Stamp)) return;

        context.RejectPrincipal();
        await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    }
}
