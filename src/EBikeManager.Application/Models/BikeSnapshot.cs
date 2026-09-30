namespace EBikeManager.Application.Models;

public sealed record BikeSnapshot(string ProfileJson, string? StateOfChargeJson, string? PassJson, string? LocationJson, bool? HasFlowPlus);
