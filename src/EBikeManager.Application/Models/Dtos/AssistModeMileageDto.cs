namespace EBikeManager.Application.Models.Dtos;

public sealed record AssistModeMileageDto(string Name, string? Color, bool IsOff, double DistanceMeters, double? EnergyWh, double Percent);
