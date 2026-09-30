using EBikeManager.Application.Utils;

namespace EBikeManager.Web.Services;

public sealed partial class PreferencesService
{
    private readonly BrowserInteropService _browser;
    private readonly ILogger<PreferencesService> _logger;

    public PreferencesService(BrowserInteropService browser, ILogger<PreferencesService> logger)
    {
        _browser = browser;
        _logger = logger;
    }

    public TimeZoneInfo TimeZone { get; private set; } = TimeZoneInfo.Utc;

    public async Task InitializeAsync()
    {
        if (await _browser.GetTimeZoneAsync() is not { } browserTimeZone) return;

        if (TimeZones.TryFindTimeZone(browserTimeZone, out var timeZone)) TimeZone = timeZone;
        else LogUnknownTimeZone(browserTimeZone);
    }

    public DateTime ToLocal(DateTime utc) => TimeZones.InTimeZone(utc, TimeZone);

    public DateTime ToUtc(DateTime wallClock) => TimeZones.WallClockToUtc(wallClock, TimeZone);

    [LoggerMessage(Level = LogLevel.Warning, Message = "The browser's time zone {TimeZone} isn't known on this server, so times are shown in UTC")]
    private partial void LogUnknownTimeZone(string timeZone);
}
