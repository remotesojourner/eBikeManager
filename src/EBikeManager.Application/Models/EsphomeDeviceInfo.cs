namespace EBikeManager.Application.Models;

public sealed record EsphomeDeviceInfo(string Name, string? FriendlyName, string? MacAddress, string? EsphomeVersion, string? ProjectName);
