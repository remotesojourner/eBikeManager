using EBikeManager.Application.Models.Events;

namespace EBikeManager.Application.Models;

public sealed record IntegrationRunResult(int Uploaded, IReadOnlyList<string> Problems, IReadOnlyList<UploadFailed>? FailedUploads = null, IReadOnlyList<string>? SignInsRequired = null)
{
    public static IntegrationRunResult Nothing { get; } = new(0, []);
}
