using System.Security.Claims;
using EBikeManager.Application.Enums;

namespace EBikeManager.Web.Utils;

public static class AccessPolicy
{
    public static Access Decide(bool signInActive, ClaimsPrincipal? user, string? stamp) =>
        !signInActive || StampMatches(signInActive, user, stamp) ? Access.Full : Access.None;

    public static bool StampMatches(bool signInActive, ClaimsPrincipal? user, string? stamp) =>
        signInActive
        && user?.Identity?.IsAuthenticated == true
        && stamp != null
        && user.FindFirst(SignInCookie.StampClaim)?.Value == stamp;

    public static string LocalReturnUrl(string? url) =>
        url != null && url.StartsWith('/') && !url.StartsWith("//", StringComparison.Ordinal) && !url.StartsWith("/\\", StringComparison.Ordinal) ? url : "/";
}
