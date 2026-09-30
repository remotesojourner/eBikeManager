using System.Collections.Frozen;
using Cronos;
using EBikeManager.Application.Enums;
using EBikeManager.Application.Resources;

namespace EBikeManager.Application.Configuration;

public static class SettingDefinitions
{
    public const string Unset = "none";

    public const string SetupCompleted = "setupCompleted";
    public const string SyncCron = "syncCron";
    public const string BoschAccount = "boschAccount";
    public const string BoschFullScan = "boschFullScan";
    public const string AuthEnabled = "authEnabled";
    public const string AuthPasswordHash = "authPasswordHash";
    public const string AuthStamp = "authStamp";

    public static IReadOnlyList<SettingDefinition> All { get; } =
    [
        new(SetupCompleted, "false", SettingVisibility.Everyone, Boolean),
        new(SyncCron, ScheduleSettings.Hourly, SettingVisibility.Everyone, Cron),
        new(BoschAccount, Unset, SettingVisibility.Everyone),
        new(BoschFullScan, "false", SettingVisibility.Everyone, Boolean),
        new(AuthEnabled, "false", SettingVisibility.SecurityTab, Boolean),
        new(AuthPasswordHash, Unset, SettingVisibility.Secret),
        new(AuthStamp, Unset, SettingVisibility.Secret)
    ];

    private static readonly FrozenDictionary<string, SettingDefinition> _byKey = All.ToFrozenDictionary(definition => definition.Key);

    public static SettingDefinition? Find(string key) => _byKey.GetValueOrDefault(key);

    private static string? Boolean(string value) =>
        value is "true" or "false" ? null : ApplicationStrings.SettingBooleanNeeded;

    private static string? Cron(string value)
    {
        try
        {
            CronExpression.Parse(value, CronFormat.Standard);
            return null;
        }
        catch (CronFormatException)
        {
            return ApplicationStrings.SettingCronInvalid;
        }
    }
}
