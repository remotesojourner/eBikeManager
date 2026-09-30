using System.Security.Cryptography;
using EBikeManager.Application.Services.Interfaces;
using Microsoft.AspNetCore.DataProtection;

namespace EBikeManager.Web.Services;

public sealed partial class DataProtectionSecretService : ISecretProtectionService
{
    private readonly IDataProtector _protector;
    private readonly ILogger<DataProtectionSecretService> _logger;

    public DataProtectionSecretService(IDataProtectionProvider dataProtection, ILogger<DataProtectionSecretService> logger)
    {
        _protector = dataProtection.CreateProtector("EBikeManager.Secrets.v1");
        _logger = logger;
    }

    public string Protect(string value) => _protector.Protect(value);

    public string? Unprotect(string protectedValue)
    {
        try
        {
            return _protector.Unprotect(protectedValue);
        }
        catch (CryptographicException ex)
        {
            LogUnreadable(ex);
            return null;
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "A stored secret couldn't be decrypted, probably because the data-protection keys changed, so it is treated as missing")]
    private partial void LogUnreadable(Exception exception);
}
