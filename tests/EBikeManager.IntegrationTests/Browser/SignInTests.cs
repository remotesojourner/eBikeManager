using System.Text.RegularExpressions;
using EBikeManager.IntegrationTests.Fixtures;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace EBikeManager.IntegrationTests.Browser;

[Collection(BrowserTestGroup.Name)]
[Trait("Category", "Browser")]
public sealed class SignInTests : BrowserTest, IClassFixture<BrowserAppWithRides>
{
    private readonly BrowserAppWithRides _app;

    public SignInTests(BrowserAppWithRides app, Chromium chromium) : base(chromium)
    {
        _app = app;
    }

    [Fact]
    public async Task TurningOnSignInSendsEveryoneToTheProviderFirst()
    {
        await using var owner = await OpenBrowserAsync(_app);
        var page = await owner.NewPageAsync();
        await page.GotoAsync("/settings/security");
        await Expect(page.GetByLabel("Redirect URI", new() { Exact = true })).ToHaveValueAsync($"{_app.BaseAddress}signin-oidc");
        await page.GetByLabel("Provider address", new() { Exact = true }).FillAsync(FakeOidcProvider.Authority);
        await page.GetByLabel("Client ID", new() { Exact = true }).FillAsync(FakeOidcProvider.ClientId);
        await page.GetByLabel("Client secret", new() { Exact = true }).FillAsync(FakeOidcProvider.ClientSecret);
        await page.GetByRole(AriaRole.Switch, new() { Name = "Require sign-in" }).CheckAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Save" }).ClickAsync();

        await Expect(page.GetByText($"Signed in as {FakeOidcProvider.UserName}.")).ToBeVisibleAsync();
        await Expect(page).ToHaveURLAsync(new Regex("/settings/security$"));
        Assert.Single(_app.Oidc.Authorizations);

        await using var visitor = await OpenBrowserAsync(_app, BrowserTheme.Light);
        var other = await visitor.NewPageAsync();
        var firstStop = await other.RunAndWaitForRequestAsync(() => other.GotoAsync("/rides"), request => request.Url.StartsWith(_app.Oidc.AuthorizeEndpoint.AbsoluteUri, StringComparison.Ordinal));
        Assert.NotNull(firstStop);
        Assert.Equal(2, _app.Oidc.Authorizations.Count);
        await Expect(other).ToHaveURLAsync(new Regex("/rides$"));
        await Expect(other.GetByText("London roundtrip").First).ToBeVisibleAsync();

        await other.GetByRole(AriaRole.Button, new() { Name = $"Sign out {FakeOidcProvider.UserName}" }).ClickAsync();
        await Expect(other.GetByText("You've signed out")).ToBeVisibleAsync();
        await other.GetByRole(AriaRole.Link, new() { Name = "Sign in again" }).ClickAsync();
        await Expect(other.GetByText("London roundtrip").First).ToBeVisibleAsync();

        await page.GetByRole(AriaRole.Switch, new() { Name = "Require sign-in" }).UncheckAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Save" }).ClickAsync();
        await Expect(page.GetByText("Sign-in settings saved.")).ToBeVisibleAsync();
        Assert.False((await _app.SettingsAsync(TestContext.Current.CancellationToken)).SignIn.IsActive);
        AssertNoBrowserErrors();
    }
}
