using EBikeManager.Application.Enums;

namespace EBikeManager.Application.Models.Dtos;

public sealed record RideExportDto(
    string RideId,
    string? RideTitle,
    DateTime LocalStartTime,
    string Integration,
    RideExportStatus Status,
    DateTime? ExportedAt,
    string? Note,
    string? Problem);
