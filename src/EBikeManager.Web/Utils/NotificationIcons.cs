namespace EBikeManager.Web.Utils;

public static class NotificationIcons
{
    public const string Folder = "img/notifications";

    private static readonly Dictionary<string, string> _files = new()
    {
        ["discord"] = "discord.svg",
        ["telegram"] = "telegram.svg",
        ["gotify"] = "gotify.svg",
        ["ntfy"] = "ntfy.svg",
        ["pushover"] = "pushover.svg",
        ["apprise"] = "apprise.webp",
        ["webhook"] = "webhook.svg"
    };

    public static IReadOnlyCollection<string> Types => _files.Keys;

    public static string For(string type) => $"{Folder}/{_files[type]}";
}
