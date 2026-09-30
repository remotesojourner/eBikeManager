using System.Net;

namespace EBikeManager.Application.Utils;

public static class GoogleHealthRedirect
{
    public static bool IsAllowed(Uri address) =>
        address.IsAbsoluteUri && (IsLoopback(address) || (address.Scheme == Uri.UriSchemeHttps && IsDomainName(address.Host)));

    private static bool IsLoopback(Uri address) =>
        (address.Scheme == Uri.UriSchemeHttp || address.Scheme == Uri.UriSchemeHttps) && address.IsLoopback;

    private static bool IsDomainName(string host) =>
        host.Contains('.', StringComparison.Ordinal) && !IPAddress.TryParse(host.Trim('[', ']'), out _) && !host.EndsWith(".local", StringComparison.OrdinalIgnoreCase);
}
