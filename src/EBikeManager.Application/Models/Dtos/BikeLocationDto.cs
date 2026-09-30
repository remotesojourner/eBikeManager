namespace EBikeManager.Application.Models.Dtos;

public sealed record BikeLocationDto(double Latitude, double Longitude, double? AccuracyMeters, DateTime? DetectedAt);
