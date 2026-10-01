namespace EBikeManager.Application.Models;

internal sealed record BridgeConfig(string Address, string Host, int Port, byte[]? EncryptionKey, string? FirstBikeId, string? SecondBikeId)
{
    public string? BikeIdFor(int slot) => slot == 2 ? SecondBikeId : FirstBikeId;
}
