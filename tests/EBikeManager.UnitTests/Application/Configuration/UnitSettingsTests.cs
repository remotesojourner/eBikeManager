using EBikeManager.Application.Configuration;
using EBikeManager.Application.Enums;

namespace EBikeManager.UnitTests.Application.Configuration;

public class UnitSettingsTests
{
    [Fact]
    public void MetricIsTheDefault()
    {
        Assert.Equal(UnitSystem.Metric, AppSettings.Defaults.Units);
    }

    [Fact]
    public void TheSavedChoiceIsRead()
    {
        var settings = AppSettings.From(new Dictionary<string, string> { [SettingDefinitions.Units] = UnitSettings.ImperialKey });

        Assert.Equal(UnitSystem.Imperial, settings.Units);
    }

    [Fact]
    public void OnlyMetricAndImperialCanBeSaved()
    {
        var definition = SettingDefinitions.Find(SettingDefinitions.Units)!;

        Assert.Null(definition.ProblemWith(UnitSettings.MetricKey));
        Assert.Null(definition.ProblemWith(UnitSettings.ImperialKey));
        Assert.NotNull(definition.ProblemWith("miles"));
        Assert.Equal(UnitSystem.Metric, AppSettings.From(new Dictionary<string, string> { [SettingDefinitions.Units] = "miles" }).Units);
    }

    [Fact]
    public void KeysRoundTrip()
    {
        Assert.All(Enum.GetValues<UnitSystem>(), units => Assert.Equal(units, UnitSettings.SystemFor(UnitSettings.KeyFor(units))));
    }

    [Theory]
    [InlineData("en-US", UnitSystem.Imperial)]
    [InlineData("en-GB", UnitSystem.Imperial)]
    [InlineData("en-gb", UnitSystem.Imperial)]
    [InlineData("en-AU", UnitSystem.Metric)]
    [InlineData("de-DE", UnitSystem.Metric)]
    [InlineData("en", UnitSystem.Metric)]
    [InlineData(null, UnitSystem.Metric)]
    public void BrowsersInTheUsAndUkAreOfferedImperial(string? language, UnitSystem expected)
    {
        Assert.Equal(expected, UnitSettings.SuggestFor(language));
    }
}
