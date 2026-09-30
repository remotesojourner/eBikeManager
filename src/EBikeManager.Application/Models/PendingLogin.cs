namespace EBikeManager.Application.Models;

public sealed record PendingLogin(string Provider, string State, string CodeVerifier, string CodeChallenge, DateTime ExpiresAt);
