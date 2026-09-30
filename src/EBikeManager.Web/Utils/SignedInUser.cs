using System.Security.Claims;

namespace EBikeManager.Web.Utils;

public static class SignedInUser
{
    public const string NameClaim = "name";

    public static string? DisplayName(ClaimsPrincipal? user) =>
        user?.FindFirst(NameClaim)?.Value
        ?? user?.FindFirst("preferred_username")?.Value
        ?? user?.FindFirst("email")?.Value
        ?? user?.FindFirst("sub")?.Value;
}
