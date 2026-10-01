namespace EBikeManager.Application.Models;

public sealed record BridgeRequest(string? Address, string? FirstBikeId, string? SecondBikeId, string? EncryptionKey, bool ClearEncryptionKey);
