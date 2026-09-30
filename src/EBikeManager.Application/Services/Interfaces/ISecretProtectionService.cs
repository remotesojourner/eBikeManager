namespace EBikeManager.Application.Services.Interfaces;

public interface ISecretProtectionService
{
    string Protect(string value);

    string? Unprotect(string protectedValue);
}
