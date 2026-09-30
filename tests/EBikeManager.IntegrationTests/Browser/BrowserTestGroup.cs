namespace EBikeManager.IntegrationTests.Browser;

[CollectionDefinition(Name)]
public sealed class BrowserTestGroup : ICollectionFixture<Chromium>
{
    public const string Name = "Browser";
}
