using EBikeManager.Application.Services.Interfaces;

namespace EBikeManager.TestSupport;

internal sealed class PlainSecretProtection : ISecretProtectionService
{
    private const string Prefix = "protected:";

    public string Protect(string value) => Prefix + value;

    public string? Unprotect(string protectedValue) =>
        protectedValue.StartsWith(Prefix, StringComparison.Ordinal) ? protectedValue[Prefix.Length..] : null;
}
