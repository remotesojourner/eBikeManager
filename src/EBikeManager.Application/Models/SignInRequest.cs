namespace EBikeManager.Application.Models;

public sealed record SignInRequest(bool Enabled, string? Authority, string? ClientId, string? ClientSecret, bool ClearClientSecret, string? Scopes);
