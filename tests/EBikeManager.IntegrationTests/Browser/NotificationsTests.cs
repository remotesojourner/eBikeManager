using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace EBikeManager.IntegrationTests.Browser;

[Collection(BrowserTestGroup.Name)]
[Trait("Category", "Browser")]
public sealed class NotificationsTests : BrowserTest, IClassFixture<BrowserAppWithRides>
{
    private readonly BrowserAppWithRides _app;

    public NotificationsTests(BrowserAppWithRides app, Chromium chromium) : base(chromium)
    {
        _app = app;
    }

    [Fact]
    public async Task ANotificationCanBeAddedTestedSavedAndDeleted()
    {
        await using var browser = await OpenBrowserAsync(_app);
        var page = await browser.NewPageAsync();

        await page.GotoAsync("/settings/notifications");
        await Expect(page.GetByText("No notifications yet")).ToBeVisibleAsync();

        await page.GetByRole(AriaRole.Button, new() { Name = "Add notification" }).ClickAsync();
        await page.GetByText("ntfy", new() { Exact = true }).Last.ClickAsync();
        await page.GetByLabel("Topic", new() { Exact = true }).FillAsync("ebike-rides");
        await page.GetByRole(AriaRole.Button, new() { Name = "Send test" }).ClickAsync();
        await Expect(page.GetByText("ntfy test sent")).ToBeVisibleAsync();
        Directory.CreateDirectory(ScreenshotFolder);
        await page.ScreenshotAsync(new() { Path = Path.Combine(ScreenshotFolder, "notifications-form.png"), FullPage = true });
        var test = Assert.Single(_app.Notifications.Requests);
        Assert.Equal("https://ntfy.sh/ebike-rides", test.Uri?.ToString());
        Assert.StartsWith("New ride: London roundtrip", test.Body, StringComparison.Ordinal);

        await page.GetByRole(AriaRole.Button, new() { Name = "Save", Exact = true }).ClickAsync();
        await Expect(page.GetByText("ntfy saved")).ToBeVisibleAsync();

        await page.ReloadAsync();
        await Expect(page.GetByText("Nothing sent yet")).ToBeVisibleAsync();
        await page.GetByText("Nothing sent yet").ClickAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Delete", Exact = true }).ClickAsync();
        await page.GetByRole(AriaRole.Dialog).GetByRole(AriaRole.Button, new() { Name = "Delete", Exact = true }).ClickAsync();
        await Expect(page.GetByText("Notification removed")).ToBeVisibleAsync();
        await Expect(page.GetByText("No notifications yet")).ToBeVisibleAsync();
        AssertNoBrowserErrors();
    }
}
