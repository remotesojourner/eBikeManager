using EBikeManager.Application.Configuration;
using EBikeManager.Application.Models;

namespace EBikeManager.Application.Repositories.Interfaces;

public interface ISettingsRepository
{
    Task<AppSettings> GetAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyDictionary<string, string>> GetValuesAsync(CancellationToken cancellationToken = default);
    Task<SettingsSaveResult> SaveAsync(IReadOnlyDictionary<string, string> changes, CancellationToken cancellationToken = default);
    Task<SettingsSaveResult> SaveSignInAsync(IReadOnlyDictionary<string, string> changes, CancellationToken cancellationToken = default);
    Task InsertDefaultsAsync(CancellationToken cancellationToken = default);
}
