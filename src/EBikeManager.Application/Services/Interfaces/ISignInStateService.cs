namespace EBikeManager.Application.Services.Interfaces;

public interface ISignInStateService
{
    bool IsActive { get; }

    Task ReloadAsync(CancellationToken cancellationToken = default);
}
