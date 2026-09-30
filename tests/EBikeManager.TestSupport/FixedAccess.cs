using EBikeManager.Application.Enums;
using EBikeManager.Application.Services.Interfaces;

namespace EBikeManager.TestSupport;

internal sealed class FixedAccess(Access level) : ICurrentAccessService
{
    public static FixedAccess Full { get; } = new(Access.Full);

    public static FixedAccess None { get; } = new(Access.None);

    public Access Level => level;

    public bool HasFullAccess => level == Access.Full;
}
