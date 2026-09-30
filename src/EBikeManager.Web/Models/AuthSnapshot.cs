using EBikeManager.Application.Configuration;

namespace EBikeManager.Web.Models;

public sealed record AuthSnapshot(SignInSettings SignIn, string? ClientSecret, bool DisabledByEnvironment)
{
    public bool IsActive => !DisabledByEnvironment && SignIn.IsActive;
}
