using EBikeManager.Application.Enums;

namespace EBikeManager.Application.Utils;

internal sealed record MessageTemplates(string RideSynced, string SyncFailed, string SyncRestored, string SignInRequired, string UploadFailed)
{
    public static MessageTemplates DiscordMarkdown { get; } = new(
        ":bike: **New ride: %title%**\n > :calendar: `Started`: %date% on %bike%\n > :straight_ruler: `Distance`: %distance% in %moving_time%\n > :dash: `Average speed`: %average_speed%\n > :mountain: `Climbed`: %elevation%\n > :fire: `Calories`: %calories% (you did %rider_share% of the work)",
        ":x: **Syncing with Bosch failed**\n > `Reason`: %error%",
        ":white_check_mark: **Syncing with Bosch works again**",
        ":key: **Sign in to %service% again**\n > eBike Manager can't reach %service% until you sign in again in its settings.",
        ":warning: **A ride didn't upload to %service%**\n > `Ride`: %title% (%date%)\n > `Reason`: %error%\n > It's tried again on the next sync.");

    public static MessageTemplates TelegramMarkdown { get; } = new(
        "🚲 *New ride: %title%*\n📅 `Started`: %date% on %bike%\n📏 `Distance`: %distance% in %moving_time%\n💨 `Average speed`: %average_speed%\n⛰️ `Climbed`: %elevation%\n🔥 `Calories`: %calories% (you did %rider_share% of the work)",
        "❌ *Syncing with Bosch failed*\n`Reason`: %error%",
        "✅ *Syncing with Bosch works again*",
        "🔑 *Sign in to %service% again*\neBike Manager can't reach %service% until you sign in again in its settings.",
        "⚠️ *A ride didn't upload to %service%*\n`Ride`: %title% (%date%)\n`Reason`: %error%\nIt's tried again on the next sync.");

    public static MessageTemplates PlainText { get; } = new(
        "New ride: %title%\nStarted %date% on %bike%\nDistance: %distance% in %moving_time%\nAverage speed: %average_speed%\nClimbed: %elevation%\nCalories: %calories% (you did %rider_share% of the work)",
        "Syncing with Bosch failed. Reason: %error%",
        "Syncing with Bosch works again.",
        "Sign in to %service% again. eBike Manager can't reach %service% until you sign in again in its settings.",
        "A ride didn't upload to %service%: %title% (%date%). Reason: %error%. It's tried again on the next sync.");

    public string For(MessageKind kind) => kind switch
    {
        MessageKind.RideSynced => RideSynced,
        MessageKind.SyncFailed => SyncFailed,
        MessageKind.SyncRestored => SyncRestored,
        MessageKind.SignInRequired => SignInRequired,
        _ => UploadFailed
    };
}
