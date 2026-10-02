using EBikeManager.Application.Enums;

namespace EBikeManager.Application.Configuration;

public sealed record AppSettings(bool SetupCompleted, ScheduleSettings Schedule, BoschSettings Bosch, SignInSettings SignIn, MapSettings Map, GoogleHealthSettings GoogleHealth, BridgeSettings Bridge, UnitSystem Units)
{
    public static AppSettings Defaults { get; } = From(SettingDefinitions.All.ToDictionary(definition => definition.Key, definition => definition.Default));

    public static AppSettings From(IReadOnlyDictionary<string, string> values)
    {
        string? Optional(string key)
        {
            var value = values.GetValueOrDefault(key);
            return string.IsNullOrWhiteSpace(value) || value == SettingDefinitions.Unset ? null : value.Trim();
        }

        string Text(string key)
        {
            var definition = SettingDefinitions.Find(key)!;
            return Optional(key) is { } value && definition.ProblemWith(value) == null ? value : definition.Default;
        }

        bool Flag(string key) => Text(key) == "true";

        return new AppSettings(
            SetupCompleted: Flag(SettingDefinitions.SetupCompleted),
            Schedule: new ScheduleSettings(Text(SettingDefinitions.SyncCron)),
            Bosch: new BoschSettings(Optional(SettingDefinitions.BoschAccount), Flag(SettingDefinitions.BoschFullScan)),
            SignIn: new SignInSettings(
                Flag(SettingDefinitions.AuthEnabled),
                Optional(SettingDefinitions.OidcAuthority),
                Optional(SettingDefinitions.OidcClientId),
                string.Join(' ', SignInSettings.ParseScopes(Text(SettingDefinitions.OidcScopes))),
                Optional(SettingDefinitions.AuthStamp)),
            Map: MapSettings.From(
                Text(SettingDefinitions.MapProvider),
                Optional(SettingDefinitions.MapStyleUrl),
                Optional(SettingDefinitions.MapDarkStyleUrl)),
            GoogleHealth: new GoogleHealthSettings(
                Optional(SettingDefinitions.GoogleHealthClientId),
                Optional(SettingDefinitions.GoogleHealthAccount),
                Optional(SettingDefinitions.GoogleHealthUploadFrom)),
            Bridge: new BridgeSettings(
                Optional(SettingDefinitions.BridgeAddress) is { } address && BridgeSettings.IsValidAddress(address) ? address : null,
                Optional(SettingDefinitions.BridgeFirstBike),
                Optional(SettingDefinitions.BridgeSecondBike)),
            Units: UnitSettings.SystemFor(Text(SettingDefinitions.Units)) ?? UnitSystem.Metric);
    }
}
