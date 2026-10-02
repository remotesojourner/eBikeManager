using EBikeManager.Application.Configuration;
using EBikeManager.Application.Enums;
using EBikeManager.Application.Models;
using EBikeManager.Application.Repositories.Interfaces;

namespace EBikeManager.TestSupport;

internal sealed class FixedSettings(UnitSystem units) : ISettingsRepository
{
    public Task<AppSettings> GetAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(AppSettings.Defaults with { Units = units });

    public Task<IReadOnlyDictionary<string, string>> GetValuesAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();

    public Task<SettingsSaveResult> SaveAsync(IReadOnlyDictionary<string, string> changes, CancellationToken cancellationToken = default) => throw new NotSupportedException();

    public Task<SettingsSaveResult> SaveSignInAsync(IReadOnlyDictionary<string, string> changes, CancellationToken cancellationToken = default) => throw new NotSupportedException();

    public Task InsertDefaultsAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
}
