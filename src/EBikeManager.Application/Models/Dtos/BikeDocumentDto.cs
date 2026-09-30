using EBikeManager.Application.Enums;

namespace EBikeManager.Application.Models.Dtos;

public sealed record BikeDocumentDto(string FileId, BikeDocumentKind Kind, string ContentType, DateTime? AddedAt, DateTime SavedAt)
{
    public bool IsImage => ContentType.StartsWith("image/", StringComparison.Ordinal);

    public static BikeDocumentDto From(BikeDocumentInfo document) =>
        new(document.FileId, KindOf(document.FileType), document.ContentType, document.AddedAt, document.SavedAt);

    private static BikeDocumentKind KindOf(string fileType) => fileType switch
    {
        "BIKE_IMAGE" => BikeDocumentKind.BikePhoto,
        "BIKE_INVOICE" => BikeDocumentKind.BikeInvoice,
        "LOCK_INVOICE" => BikeDocumentKind.LockInvoice,
        _ => BikeDocumentKind.Other
    };
}
