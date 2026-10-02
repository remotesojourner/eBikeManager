using EBikeManager.Application.Models.Events;
using EBikeManager.Application.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EBikeManager.Application.BackgroundServices;

internal sealed partial class NotificationTickerService : BackgroundService
{
    private static readonly TimeSpan _interval = TimeSpan.FromMinutes(1);

    private readonly IServiceScopeFactory _scopes;
    private readonly TimeProvider _time;
    private readonly ILogger<NotificationTickerService> _logger;

    public NotificationTickerService(IServiceScopeFactory scopes, TimeProvider time, ILogger<NotificationTickerService> logger)
    {
        _scopes = scopes;
        _time = time;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(_interval, _time);
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await TickAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
    }

    private async Task TickAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var scope = _scopes.CreateScope();
            await scope.ServiceProvider.GetRequiredService<INotificationDispatchService>().PublishAsync(new Heartbeat(), stoppingToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogTickFailed(ex);
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "The keep-alive notifications could not be sent")]
    private partial void LogTickFailed(Exception exception);
}
