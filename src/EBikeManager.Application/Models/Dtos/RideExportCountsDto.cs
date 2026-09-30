namespace EBikeManager.Application.Models.Dtos;

public sealed record RideExportCountsDto(int Uploaded, int Failed, int WithNotes);
