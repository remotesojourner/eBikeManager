using EBikeManager.Application.Configuration;
using EBikeManager.Application.Models.Entities;
using EBikeManager.Application.Repositories;
using EBikeManager.IntegrationTests.Fixtures;

namespace EBikeManager.IntegrationTests.Application.Repositories;

public sealed class SettingsRepositoryTests : IDisposable
{
    private readonly TestDatabase _database = new();

    public void Dispose() => _database.Dispose();

    [Fact]
    public async Task DefaultsAreInsertedOnceAndReadBackTyped()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var db = _database.NewContext();
        var repository = new SettingsRepository(db);

        await repository.InsertDefaultsAsync(cancellationToken);
        await repository.InsertDefaultsAsync(cancellationToken);

        Assert.Equal(SettingDefinitions.All.Count, db.Configs.Count());
        Assert.Equal(AppSettings.Defaults, await repository.GetAsync(cancellationToken));
    }

    [Fact]
    public async Task TheRetiredPasswordIsDeletedAtStartup()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var db = _database.NewContext();
        db.Configs.Add(new ConfigEntry { Key = "authPasswordHash", Value = "v1.600000.salt.hash" });
        await db.SaveChangesAsync(cancellationToken);

        await new SettingsRepository(db).InsertDefaultsAsync(cancellationToken);

        Assert.DoesNotContain(db.Configs, entry => entry.Key == "authPasswordHash");
        Assert.Equal(SettingDefinitions.All.Count, db.Configs.Count());
    }

    [Fact]
    public async Task ASaveIsValidatedAsAWholeBeforeAnythingIsWritten()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var db = _database.NewContext();
        var repository = new SettingsRepository(db);

        var refused = await repository.SaveAsync(new Dictionary<string, string>
        {
            [SettingDefinitions.SetupCompleted] = "true",
            [SettingDefinitions.SyncCron] = "not a cron"
        }, cancellationToken);
        var saved = await repository.SaveAsync(new Dictionary<string, string> { [SettingDefinitions.SyncCron] = "*/30 * * * *" }, cancellationToken);

        Assert.False(refused.Succeeded);
        Assert.True(saved.Succeeded);
        var settings = await repository.GetAsync(cancellationToken);
        Assert.False(settings.SetupCompleted);
        Assert.Equal("*/30 * * * *", settings.Schedule.Cron);
    }

    [Fact]
    public async Task SignInSettingsAreOnlySavedThroughTheSecurityTab()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var db = _database.NewContext();
        var repository = new SettingsRepository(db);

        Assert.False((await repository.SaveAsync(new Dictionary<string, string> { [SettingDefinitions.AuthEnabled] = "true" }, cancellationToken)).Succeeded);
        Assert.False((await repository.SaveSignInAsync(new Dictionary<string, string> { [SettingDefinitions.SyncCron] = ScheduleSettings.Hourly }, cancellationToken)).Succeeded);
        Assert.False((await repository.SaveAsync(new Dictionary<string, string> { ["unknown"] = "x" }, cancellationToken)).Succeeded);
    }
}
