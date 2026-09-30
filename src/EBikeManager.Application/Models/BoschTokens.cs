namespace EBikeManager.Application.Models;

public sealed record BoschTokens(string AccessToken, string RefreshToken, DateTime AccessTokenExpiresAt, string? AccountName);
