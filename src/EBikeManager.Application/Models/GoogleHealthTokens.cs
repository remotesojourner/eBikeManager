namespace EBikeManager.Application.Models;

public sealed record GoogleHealthTokens(string AccessToken, string RefreshToken, DateTime AccessTokenExpiresAt, string? AccountName);
