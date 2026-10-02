using EBikeManager.Application.Models.Dtos;
using EBikeManager.Web.Utils;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace EBikeManager.Web.Services;

public sealed partial class MapViewService
{
    private readonly IJSRuntime _js;
    private readonly ILogger<MapViewService> _logger;

    public MapViewService(IJSRuntime js, ILogger<MapViewService> logger)
    {
        _js = js;
        _logger = logger;
    }

    public Task ShowRideAsync(string viewId, ElementReference? map, ElementReference? charts, ElementReference? readout, RideTrackDto track, MapSource source, ChartUnits units, string? colourBy) =>
        InvokeAsync("ebikeManagerMaps.showRide", viewId, map, charts, readout, track, source, MapTexts.Current, units, colourBy);

    public Task SetRouteColourAsync(string viewId, string? colourBy) =>
        InvokeAsync("ebikeManagerMaps.setColour", viewId, colourBy);

    public Task ShowMapAsync(string viewId, ElementReference map, IReadOnlyList<double[]> route, MapSource source) =>
        InvokeAsync("ebikeManagerMaps.showMap", viewId, map, route, source, MapTexts.Current);

    public Task SetSourceAsync(string viewId, MapSource source) =>
        InvokeAsync("ebikeManagerMaps.setSource", viewId, source);

    public Task DisposeViewAsync(string viewId) =>
        InvokeAsync("ebikeManagerMaps.dispose", viewId);

    private async Task InvokeAsync(string identifier, params object?[] arguments)
    {
        try
        {
            await _js.InvokeVoidAsync(identifier, arguments);
        }
        catch (Exception ex) when (FailureLevel(ex) is { } level)
        {
            LogMapFailed(level, ex, identifier);
        }
    }

    private static LogLevel? FailureLevel(Exception ex) => ex switch
    {
        JSDisconnectedException or TaskCanceledException => LogLevel.Debug,
        JSException => LogLevel.Warning,
        _ => null
    };

    [LoggerMessage(Message = "The browser couldn't run {Identifier}")]
    private partial void LogMapFailed(LogLevel level, Exception exception, string identifier);
}
