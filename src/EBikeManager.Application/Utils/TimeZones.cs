namespace EBikeManager.Application.Utils;

public static class TimeZones
{
    public static DateTime AsUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };

    public static DateTime InTimeZone(DateTime value, TimeZoneInfo timeZone) =>
        TimeZoneInfo.ConvertTimeFromUtc(AsUtc(value), timeZone);

    public static DateTime WallClockToUtc(DateTime wallClock, TimeZoneInfo timeZone)
    {
        var local = DateTime.SpecifyKind(wallClock, DateTimeKind.Unspecified);
        while (timeZone.IsInvalidTime(local))
        {
            local = local.AddMinutes(15);
        }

        return TimeZoneInfo.ConvertTimeToUtc(local, timeZone);
    }

    public static bool TryFindTimeZone(string id, out TimeZoneInfo timeZone)
    {
        var found = TimeZoneInfo.TryFindSystemTimeZoneById(id, out var match);
        timeZone = match ?? TimeZoneInfo.Utc;
        return found;
    }

    public static DateTime ToRideLocal(DateTime utc, string? timeZoneId) =>
        timeZoneId != null && TryFindTimeZone(timeZoneId, out var zone) ? InTimeZone(utc, zone) : DateTime.SpecifyKind(AsUtc(utc), DateTimeKind.Unspecified);
}
