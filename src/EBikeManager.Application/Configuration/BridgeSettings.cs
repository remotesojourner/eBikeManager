using System.Globalization;

namespace EBikeManager.Application.Configuration;

public sealed record BridgeSettings(string? Address, string? FirstBikeId, string? SecondBikeId)
{
    public const string SetupUrl = "https://xunil99.github.io/ha-bosch-ebike/";
    public const int DefaultPort = 6053;
    public const int KeyLength = 32;

    public bool IsOn => Address != null;

    public string? BikeIdFor(int slot) => slot == 2 ? SecondBikeId : FirstBikeId;

    public static bool IsValidAddress(string value) => TryParseAddress(value, out _, out _);

    public static bool TryParseAddress(string value, out string host, out int port)
    {
        host = value.Trim();
        port = DefaultPort;
        var colon = host.LastIndexOf(':');
        if (colon >= 0)
        {
            if (host.IndexOf(':', StringComparison.Ordinal) != colon) return false;
            if (!int.TryParse(host[(colon + 1)..], NumberStyles.None, CultureInfo.InvariantCulture, out port) || port is < 1 or > 65535) return false;
            host = host[..colon];
        }

        return Uri.CheckHostName(host) is UriHostNameType.Dns or UriHostNameType.IPv4;
    }

    public static bool TryReadKey(string? value, out byte[] key)
    {
        key = [];
        if (string.IsNullOrWhiteSpace(value)) return false;

        var buffer = new byte[KeyLength + 3];
        if (!Convert.TryFromBase64String(value.Trim(), buffer, out var written) || written != KeyLength) return false;

        key = buffer[..KeyLength];
        return true;
    }
}
