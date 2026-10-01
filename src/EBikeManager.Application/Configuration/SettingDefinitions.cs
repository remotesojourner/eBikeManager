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
    public const string OidcAuthority = "oidcAuthority";
    public const string OidcClientId = "oidcClientId";
    public const string OidcScopes = "oidcScopes";
    public const string AuthStamp = "authStamp";
    public const string MapProvider = "mapProvider";
    public const string MapStyleUrl = "mapStyleUrl";
    public const string MapDarkStyleUrl = "mapDarkStyleUrl";
    public const string GoogleHealthClientId = "googleHealthClientId";
    public const string GoogleHealthAccount = "googleHealthAccount";
    public const string GoogleHealthUploadFrom = "googleHealthUploadFrom";
    public const string BridgeAddress = "bridgeAddress";
    public const string BridgeFirstBike = "bridgeFirstBike";
    public const string BridgeSecondBike = "bridgeSecondBike";

    public static IReadOnlyList<SettingDefinition> All { get; } =
    [
        new(SetupCompleted, "false", SettingVisibility.Everyone, Boolean),
        new(SyncCron, ScheduleSettings.Hourly, SettingVisibility.Everyone, Cron),
        new(BoschAccount, Unset, SettingVisibility.Everyone),
        new(BoschFullScan, "false", SettingVisibility.Everyone, Boolean),
        new(AuthEnabled, "false", SettingVisibility.SecurityTab, Boolean),
        new(OidcAuthority, Unset, SettingVisibility.SecurityTab, Authority),
        new(OidcClientId, Unset, SettingVisibility.SecurityTab),
        new(OidcScopes, SignInSettings.DefaultScopes, SettingVisibility.SecurityTab),
        new(AuthStamp, Unset, SettingVisibility.Secret),
        new(MapProvider, MapSettings.OpenStreetMapKey, SettingVisibility.Everyone, MapProviderKey),
        new(MapStyleUrl, Unset, SettingVisibility.Everyone, StyleUrl),
        new(MapDarkStyleUrl, Unset, SettingVisibility.Everyone, StyleUrl),
        new(GoogleHealthClientId, Unset, SettingVisibility.Everyone, GoogleClientId),
        new(GoogleHealthAccount, Unset, SettingVisibility.Everyone),
        new(GoogleHealthUploadFrom, Unset, SettingVisibility.Everyone, UploadFrom),
        new(BridgeAddress, Unset, SettingVisibility.Everyone, BridgeAddressValue),
        new(BridgeFirstBike, Unset, SettingVisibility.Everyone),
        new(BridgeSecondBike, Unset, SettingVisibility.Everyone)
    ];

    public static IReadOnlyList<string> ObsoleteKeys { get; } = ["authPasswordHash"];

    private static readonly FrozenDictionary<string, SettingDefinition> _byKey = All.ToFrozenDictionary(definition => definition.Key);

    public static SettingDefinition? Find(string key) => _byKey.GetValueOrDefault(key);

    private static string? Boolean(string value) =>
        value is "true" or "false" ? null : ApplicationStrings.SettingBooleanNeeded;

    private static string? Authority(string value) =>
        value == Unset || SignInSettings.IsValidAuthority(value) ? null : ApplicationStrings.SignInProviderUrlInvalid;

    private static string? MapProviderKey(string value) =>
        MapSettings.ProviderFor(value) != null ? null : ApplicationStrings.SettingMapProviderInvalid;

    private static string? StyleUrl(string value) =>
        value == Unset || MapSettings.IsValidStyleUrl(value) ? null : ApplicationStrings.SettingMapStyleUrlInvalid;

    private static string? GoogleClientId(string value) =>
        value == Unset || GoogleHealthSettings.IsValidClientId(value) ? null : ApplicationStrings.GoogleHealthClientIdInvalid;

    private static string? UploadFrom(string value) =>
        value == Unset || GoogleHealthSettings.IsValidUploadFrom(value) ? null : ApplicationStrings.GoogleHealthUploadChoiceInvalid;

    private static string? BridgeAddressValue(string value) =>
        value == Unset || BridgeSettings.IsValidAddress(value) ? null : ApplicationStrings.BridgeAddressInvalid;

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
