using EBikeManager.Application.Configuration;

namespace EBikeManager.UnitTests.Application.Configuration;

public class GoogleHealthSettingsTests
{
    [Theory]
    [InlineData("123456789012-abcdefghijklmnop.apps.googleusercontent.com", true)]
    [InlineData(" 123456789012-abcdefghijklmnop.apps.googleusercontent.com ", true)]
    [InlineData("apps.googleusercontent.com", false)]
    [InlineData("123456789012-abcdefghijklmnop.example.com", false)]
    public void ClientIdsLookLikeGoogleOAuthClients(string clientId, bool valid)
    {
        Assert.Equal(valid, GoogleHealthSettings.IsValidClientId(clientId));
    }

    [Fact]
    public void TheUploadChoiceIsAllRidesOrAStartInstant()
    {
        var since = new DateTime(2026, 9, 1, 23, 0, 0, DateTimeKind.Utc);
        var value = GoogleHealthSettings.UploadFromValue(since);

        Assert.Equal("2026-09-01T23:00:00Z", value);
        Assert.Equal(since, new GoogleHealthSettings(null, null, value).UploadFromUtc);
        Assert.True(new GoogleHealthSettings(null, null, GoogleHealthSettings.AllRides) is { UploadsAllRides: true, HasUploadChoice: true, UploadFromUtc: null });
        Assert.False(new GoogleHealthSettings(null, null, null).HasUploadChoice);
        Assert.False(GoogleHealthSettings.IsValidUploadFrom("yesterday"));
    }

    [Fact]
    public void StoredGoogleHealthSettingsAreRead()
    {
        var values = SettingDefinitions.All.ToDictionary(definition => definition.Key, definition => definition.Default);
        values[SettingDefinitions.GoogleHealthClientId] = "123456789012-abcdefghijklmnop.apps.googleusercontent.com";
        values[SettingDefinitions.GoogleHealthAccount] = "rider@gmail.com";
        values[SettingDefinitions.GoogleHealthUploadFrom] = GoogleHealthSettings.AllRides;

        var google = AppSettings.From(values).GoogleHealth;

        Assert.Equal(new GoogleHealthSettings("123456789012-abcdefghijklmnop.apps.googleusercontent.com", "rider@gmail.com", GoogleHealthSettings.AllRides), google);
        Assert.Equal(new GoogleHealthSettings(null, null, null), AppSettings.Defaults.GoogleHealth);
    }

    [Theory]
    [InlineData(SettingDefinitions.GoogleHealthClientId, "not-a-client", false)]
    [InlineData(SettingDefinitions.GoogleHealthUploadFrom, "2026-09-01T00:00:00Z", true)]
    [InlineData(SettingDefinitions.GoogleHealthUploadFrom, "2026-09-01", false)]
    public void GoogleHealthSettingsAreCheckedBeforeTheyAreSaved(string key, string value, bool accepted)
    {
        Assert.Equal(accepted, SettingDefinitions.Find(key)!.ProblemWith(value) == null);
    }
}
