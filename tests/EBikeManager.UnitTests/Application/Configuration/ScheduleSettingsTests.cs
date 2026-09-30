using EBikeManager.Application.Configuration;

namespace EBikeManager.UnitTests.Application.Configuration;

public class ScheduleSettingsTests
{
    [Fact]
    public void TheDefaultScheduleSyncsEveryHourOnTheHour()
    {
        var schedule = AppSettings.Defaults.Schedule;

        Assert.Equal(ScheduleSettings.Hourly, schedule.Cron);
        Assert.Equal(new DateTime(2026, 9, 30, 13, 0, 0, DateTimeKind.Utc), schedule.NextRunAfter(new DateTime(2026, 9, 30, 12, 34, 0, DateTimeKind.Utc)));
    }

    [Theory]
    [InlineData("0 * * * *", true)]
    [InlineData("*/30 * * * *", true)]
    [InlineData("every hour", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void RecognisesValidCronExpressions(string? cron, bool valid)
    {
        Assert.Equal(valid, ScheduleSettings.IsValid(cron));
    }

    [Fact]
    public void InvalidStoredValuesFallBackToTheirDefaults()
    {
        var settings = AppSettings.From(new Dictionary<string, string>
        {
            [SettingDefinitions.SyncCron] = "not a cron",
            [SettingDefinitions.SetupCompleted] = "true",
            [SettingDefinitions.BoschAccount] = SettingDefinitions.Unset
        });

        Assert.Equal(ScheduleSettings.Hourly, settings.Schedule.Cron);
        Assert.True(settings.SetupCompleted);
        Assert.Null(settings.Bosch.AccountName);
    }
}
