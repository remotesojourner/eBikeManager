using EBikeManager.Application.Enums;

namespace EBikeManager.Application.Services.Interfaces;

public interface ICurrentAccessService
{
    Access Level { get; }

    bool HasFullAccess { get; }
}
