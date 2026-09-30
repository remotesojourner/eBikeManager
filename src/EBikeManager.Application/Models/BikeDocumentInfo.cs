namespace EBikeManager.Application.Models;

public sealed record BikeDocumentInfo(string BikeId, string FileId, string FileType, string ContentType, DateTime? AddedAt, DateTime? SourceUpdatedAt, DateTime SavedAt);
