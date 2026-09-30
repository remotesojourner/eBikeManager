using EBikeManager.Application.Enums;

namespace EBikeManager.Application.Models.Dtos;

public sealed record BikeComponentDto(BikeComponentKind Kind, string? ProductName, string? SoftwareVersion, string? SerialNumber, string? PartNumber, string? HardwareVersion);
