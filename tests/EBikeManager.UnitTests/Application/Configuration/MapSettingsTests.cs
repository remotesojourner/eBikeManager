using EBikeManager.Application.Configuration;
using EBikeManager.Application.Enums;

namespace EBikeManager.UnitTests.Application.Configuration;

public class MapSettingsTests
{
    private const string Style = "https://maps.example.test/styles/liberty";
    private const string DarkStyle = "https://maps.example.test/styles/dark";

    [Fact]
    public void OpenStreetMapIsTheDefault()
    {
        Assert.Equal(new MapSettings(MapProvider.OpenStreetMap, null, null), AppSettings.Defaults.Map);
        Assert.Null(AppSettings.Defaults.Map.StyleFor(dark: true));
    }

    [Fact]
    public void OpenFreeMapFollowsTheTheme()
    {
        var map = MapSettings.From(MapSettings.OpenFreeMapKey, null, null);

        Assert.Equal(MapSettings.OpenFreeMapStyle, map.StyleFor(dark: false));
        Assert.Equal(MapSettings.OpenFreeMapDarkStyle, map.StyleFor(dark: true));
    }

    [Fact]
    public void YourOwnServerUsesItsDarkStyleOnlyWhenOneIsGiven()
    {
        var withDark = MapSettings.From(MapSettings.CustomKey, $" {Style} ", DarkStyle);
        var withoutDark = MapSettings.From(MapSettings.CustomKey, Style, null);

        Assert.Equal((Style, DarkStyle), (withDark.StyleFor(dark: false), withDark.StyleFor(dark: true)));
        Assert.Equal(Style, withoutDark.StyleFor(dark: true));
    }

    [Fact]
    public void YourOwnServerWithoutAUsableStyleFallsBackToOpenStreetMap()
    {
        Assert.Equal(MapProvider.OpenStreetMap, MapSettings.From(MapSettings.CustomKey, null, null).Provider);
        Assert.Equal(MapProvider.OpenStreetMap, MapSettings.From(MapSettings.CustomKey, "ftp://maps.example.test/style.json", null).Provider);
        Assert.Equal(MapProvider.OpenStreetMap, MapSettings.From("somewhere", Style, null).Provider);
    }

    [Theory]
    [InlineData(SettingDefinitions.MapProvider, "openFreeMap", true)]
    [InlineData(SettingDefinitions.MapProvider, "google", false)]
    [InlineData(SettingDefinitions.MapStyleUrl, "http://maps.lan:8080/styles/liberty", true)]
    [InlineData(SettingDefinitions.MapStyleUrl, SettingDefinitions.Unset, true)]
    [InlineData(SettingDefinitions.MapDarkStyleUrl, "styles/dark", false)]
    public void MapSettingsAreCheckedBeforeTheyAreSaved(string key, string value, bool accepted)
    {
        Assert.Equal(accepted, SettingDefinitions.Find(key)!.ProblemWith(value) == null);
    }

    [Fact]
    public void StoredMapSettingsAreRead()
    {
        var values = SettingDefinitions.All.ToDictionary(definition => definition.Key, definition => definition.Default);
        values[SettingDefinitions.MapProvider] = MapSettings.CustomKey;
        values[SettingDefinitions.MapStyleUrl] = Style;

        Assert.Equal(new MapSettings(MapProvider.Custom, Style, null), AppSettings.From(values).Map);
    }
}
