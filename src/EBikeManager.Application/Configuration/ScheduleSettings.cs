using Cronos;

namespace EBikeManager.Application.Configuration;

public sealed record ScheduleSettings(string Cron)
{
    public const string Hourly = "0 * * * *";

    public static bool IsValid(string? cron)
    {
        if (string.IsNullOrWhiteSpace(cron)) return false;

        try
        {
            CronExpression.Parse(cron, CronFormat.Standard);
            return true;
        }
        catch (CronFormatException)
        {
            return false;
        }
    }

    public DateTime? NextRunAfter(DateTime utc) =>
        CronExpression.Parse(Cron, CronFormat.Standard).GetNextOccurrence(DateTime.SpecifyKind(utc, DateTimeKind.Utc), TimeZoneInfo.Utc);
}
