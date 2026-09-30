namespace EBikeManager.Application.Configuration;

public sealed record SignInSettings(bool Enabled, string? PasswordHash, string? Stamp)
{
    public bool IsActive => Enabled && PasswordHash != null;
}
