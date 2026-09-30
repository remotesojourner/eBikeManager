using System.Globalization;
using EBikeManager.Application.Utils;

namespace EBikeManager.Application.Configuration;

public sealed record GoogleHealthSettings(string? ClientId, string? AccountName, string? UploadFrom)
{
    public const string AllRides = "all";

    private const string InstantFormat = "yyyy-MM-dd'T'HH:mm:ss'Z'";

    public bool UploadsAllRides => UploadFrom == AllRides;

    public DateTime? UploadFromUtc => UploadFrom is { } value && TryReadInstant(value, out var instant) ? instant : null;

    public bool HasUploadChoice => UploadsAllRides || UploadFromUtc != null;

    public static string UploadFromValue(DateTime utc) => utc.ToString(InstantFormat, CultureInfo.InvariantCulture);

    public static bool IsValidUploadFrom(string value) => value == AllRides || TryReadInstant(value, out _);

    public static bool IsValidClientId(string value) =>
        value.Trim() is { Length: > 30 } id && id.EndsWith(GoogleHealthEndpoints.ClientIdSuffix, StringComparison.Ordinal) && !id.Contains(' ', StringComparison.Ordinal);

    private static bool TryReadInstant(string value, out DateTime instant) =>
        DateTime.TryParseExact(value, InstantFormat, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out instant);
}
