namespace EBikeManager.Application.Utils;

public static class DocumentFormat
{
    public const int MaxBytes = 25 * 1024 * 1024;

    public const string Pdf = "application/pdf";

    public static string? ContentTypeOf(ReadOnlySpan<byte> content) =>
        ImageFormat.ContentTypeOf(content) ?? (content.StartsWith("%PDF-"u8) ? Pdf : null);
}
