using EBikeManager.Application.Enums;

namespace EBikeManager.Application.Models.Entities;

public class RideExport
{
    public string RideId { get; set; } = string.Empty;

    public string Integration { get; set; } = string.Empty;

    public RideExportStatus Status { get; set; }

    public string? RemoteId { get; set; }

    public string? Note { get; set; }

    public string? Problem { get; set; }

    public int Attempts { get; set; }

    public DateTime LastAttemptAt { get; set; }

    public DateTime? ExportedAt { get; set; }
}
