namespace EBikeManager.Application.Models.Entities;

public class BikeDocument
{
    public string BikeId { get; set; } = string.Empty;

    public string FileId { get; set; } = string.Empty;

    public string FileType { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    public byte[] Content { get; set; } = [];

    public DateTime? AddedAt { get; set; }

    public DateTime? SourceUpdatedAt { get; set; }

    public DateTime SavedAt { get; set; }
}
