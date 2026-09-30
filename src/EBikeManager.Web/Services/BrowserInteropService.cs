using Microsoft.JSInterop;

namespace EBikeManager.Web.Services;

public sealed partial class BrowserInteropService
{
    private readonly IJSRuntime _js;
    private readonly ILogger<BrowserInteropService> _logger;

    public BrowserInteropService(IJSRuntime js, ILogger<BrowserInteropService> logger)
    {
        _js = js;
        _logger = logger;
    }

    public async Task<string?> GetLocalStorageAsync(string key)
    {
        try
        {
            return await _js.InvokeAsync<string?>("ebikeManagerInterop.getLocalStorage", key);
        }
        catch (Exception ex) when (FailureLevel(ex) is { } level)
        {
            LogReadFailed(level, ex, key);
            return null;
        }
    }

    public async Task SetLocalStorageAsync(string key, string value)
    {
        try
        {
            await _js.InvokeVoidAsync("ebikeManagerInterop.setLocalStorage", key, value);
        }
        catch (Exception ex) when (FailureLevel(ex) is { } level)
        {
            LogSaveFailed(level, ex, key);
        }
    }

    public async Task<string?> GetTimeZoneAsync()
    {
        try
        {
            return await _js.InvokeAsync<string?>("ebikeManagerInterop.getTimeZone");
        }
        catch (Exception ex) when (FailureLevel(ex) is { } level)
        {
            LogTimeZoneFailed(level, ex);
            return null;
        }
    }

    public async Task<bool> DownloadAsync(string fileName, byte[] content)
    {
        try
        {
            using var stream = new MemoryStream(content);
            using var reference = new DotNetStreamReference(stream);
            await _js.InvokeVoidAsync("ebikeManagerInterop.downloadFileFromStream", fileName, reference);
            return true;
        }
        catch (Exception ex) when (FailureLevel(ex) is { } level)
        {
            LogDownloadFailed(level, ex, fileName);
            return false;
        }
    }

    public async Task<bool> CopyTextAsync(string text)
    {
        try
        {
            await _js.InvokeVoidAsync("ebikeManagerInterop.copyText", text);
            return true;
        }
        catch (Exception ex) when (FailureLevel(ex) is { } level)
        {
            LogCopyFailed(level, ex);
            return false;
        }
    }

    private static LogLevel? FailureLevel(Exception ex) => ex switch
    {
        JSDisconnectedException or TaskCanceledException => LogLevel.Debug,
        JSException => LogLevel.Warning,
        _ => null
    };

    [LoggerMessage(Message = "Could not read '{Key}' from the browser's storage")]
    private partial void LogReadFailed(LogLevel level, Exception exception, string key);

    [LoggerMessage(Message = "Could not save '{Key}' to the browser's storage")]
    private partial void LogSaveFailed(LogLevel level, Exception exception, string key);

    [LoggerMessage(Message = "Could not read the browser's time zone")]
    private partial void LogTimeZoneFailed(LogLevel level, Exception exception);

    [LoggerMessage(Message = "Could not copy to the clipboard")]
    private partial void LogCopyFailed(LogLevel level, Exception exception);

    [LoggerMessage(Message = "Could not download {FileName}")]
    private partial void LogDownloadFailed(LogLevel level, Exception exception, string fileName);
}
