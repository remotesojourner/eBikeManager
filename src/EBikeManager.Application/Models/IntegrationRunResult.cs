namespace EBikeManager.Application.Models;

public sealed record IntegrationRunResult(int Uploaded, IReadOnlyList<string> Problems)
{
    public static IntegrationRunResult Nothing { get; } = new(0, []);
}
