using EBikeManager.Application.Enums;

namespace EBikeManager.Application.Models;

public sealed record RideExportOutcome(RideExportStatus Status, string? RemoteId, string? Note);
