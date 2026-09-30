using EBikeManager.Application.Configuration;
using EBikeManager.Application.Data;
using EBikeManager.Application.Models;
using EBikeManager.Application.Models.Entities;
using EBikeManager.Application.Repositories.Interfaces;
using EBikeManager.Application.Resources;
using Microsoft.EntityFrameworkCore;

namespace EBikeManager.Application.Repositories;

internal sealed class SettingsRepository : ISettingsRepository
{
    private readonly EBikeManagerDbContext _db;

    public SettingsRepository(EBikeManagerDbContext db)
    {
        _db = db;
    }

    public async Task<AppSettings> GetAsync(CancellationToken cancellationToken = default) =>
        AppSettings.From(await GetValuesAsync(cancellationToken));

    public async Task<IReadOnlyDictionary<string, string>> GetValuesAsync(CancellationToken cancellationToken = default)
    {
        var stored = await _db.Configs.AsNoTracking().ToDictionaryAsync(entry => entry.Key, entry => entry.Value, cancellationToken);
        return SettingDefinitions.All.ToDictionary(definition => definition.Key, definition => stored.GetValueOrDefault(definition.Key, definition.Default));
    }

    public Task<SettingsSaveResult> SaveAsync(IReadOnlyDictionary<string, string> changes, CancellationToken cancellationToken = default) =>
        WriteAsync(changes, definition => !definition.IsManagedOnSecurityTab, ApplicationStrings.SettingsOnSecurityTab, cancellationToken);

    public Task<SettingsSaveResult> SaveSignInAsync(IReadOnlyDictionary<string, string> changes, CancellationToken cancellationToken = default) =>
        WriteAsync(changes, definition => definition.IsManagedOnSecurityTab, ApplicationStrings.SettingsOnlySignIn, cancellationToken);

    public async Task InsertDefaultsAsync(CancellationToken cancellationToken = default)
    {
        var existingKeys = (await _db.Configs.Select(entry => entry.Key).ToListAsync(cancellationToken)).ToHashSet();

        foreach (var definition in SettingDefinitions.All.Where(definition => !existingKeys.Contains(definition.Key)))
        {
            _db.Configs.Add(new ConfigEntry { Key = definition.Key, Value = definition.Default });
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task<SettingsSaveResult> WriteAsync(
        IReadOnlyDictionary<string, string> changes,
        Func<SettingDefinition, bool> changeableHere,
        string changedElsewhere,
        CancellationToken cancellationToken)
    {
        if (changes.Count == 0) return new SettingsSaveResult(ApplicationStrings.SettingsNoChanges);

        foreach (var (key, value) in changes)
        {
            if (SettingDefinitions.Find(key) is not { } definition) return new SettingsSaveResult(ApplicationStrings.Format(ApplicationStrings.SettingsUnknownKey, key));
            if (!changeableHere(definition)) return new SettingsSaveResult(changedElsewhere);
            if (definition.ProblemWith(value) is { } problem) return new SettingsSaveResult(problem);
        }

        var keys = changes.Keys.ToList();
        var stored = await _db.Configs.Where(entry => keys.Contains(entry.Key)).ToDictionaryAsync(entry => entry.Key, cancellationToken);
        foreach (var (key, value) in changes)
        {
            if (stored.TryGetValue(key, out var entry)) entry.Value = value;
            else _db.Configs.Add(new ConfigEntry { Key = key, Value = value });
        }

        await _db.SaveChangesAsync(cancellationToken);
        return SettingsSaveResult.Saved;
    }
}
