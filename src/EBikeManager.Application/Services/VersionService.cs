using EBikeManager.Application.Models.Dtos;
using EBikeManager.Application.Services.Interfaces;
using EBikeManager.Application.Utils;

namespace EBikeManager.Application.Services;

public sealed class VersionService
{
    private readonly IReleaseService _releases;

    public VersionService(IReleaseService releases)
    {
        _releases = releases;
    }

    public async Task<VersionInfoDto> GetVersionAsync(CancellationToken cancellationToken = default) => new()
    {
        Local = ProjectInfo.Version,
        Remote = await _releases.GetLatestVersionAsync(cancellationToken) ?? "0"
    };
}
