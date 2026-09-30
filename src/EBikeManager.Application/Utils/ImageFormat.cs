namespace EBikeManager.Application.Utils;

public static class ImageFormat
{
    public const int MaxBytes = 10 * 1024 * 1024;

    private static ReadOnlySpan<byte> Png => [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    private static ReadOnlySpan<byte> Jpeg => [0xFF, 0xD8, 0xFF];

    public static string? ContentTypeOf(ReadOnlySpan<byte> content)
    {
        if (content.StartsWith(Png)) return "image/png";
        if (content.StartsWith(Jpeg)) return "image/jpeg";
        if (content.StartsWith("GIF87a"u8) || content.StartsWith("GIF89a"u8)) return "image/gif";
        if (content.Length >= 12 && content.StartsWith("RIFF"u8) && content[8..12].SequenceEqual("WEBP"u8)) return "image/webp";
        return null;
    }
}
