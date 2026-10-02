using System.Runtime.CompilerServices;
using EBikeManager.TestSupport;
using EBikeManager.Web.Utils;

namespace EBikeManager.UnitTests.Web.Utils;

public class NotificationIconsTests
{
    [Fact]
    public void EveryNotificationTypeHasAnIconAndEveryIconFileIsUsed()
    {
        using var handler = new RecordingHandler(_ => new HttpResponseMessage());
        var types = TestNotifications.All(handler).Select(type => type.Name).ToList();
        var webRoot = WebRoot();

        Assert.Equal(types.Order(StringComparer.Ordinal), NotificationIcons.Types.Order(StringComparer.Ordinal));

        var expectedFiles = types.Select(type => Path.GetFullPath(Path.Combine(webRoot, NotificationIcons.For(type)))).Order(StringComparer.Ordinal);
        var actualFiles = Directory.GetFiles(Path.Combine(webRoot, NotificationIcons.Folder)).Select(Path.GetFullPath).Order(StringComparer.Ordinal);
        Assert.Equal(expectedFiles, actualFiles);
    }

    private static string WebRoot([CallerFilePath] string testFile = "") =>
        Path.Combine(Path.GetDirectoryName(testFile)!, "..", "..", "..", "..", "src", "EBikeManager.Web", "wwwroot");
}
