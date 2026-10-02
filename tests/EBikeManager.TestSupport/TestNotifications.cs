using EBikeManager.Application.Enums;
using EBikeManager.Application.Installers;
using EBikeManager.Application.Repositories.Interfaces;
using EBikeManager.Application.Services;
using EBikeManager.Application.Services.Notifications.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace EBikeManager.TestSupport;

internal static class TestNotifications
{
    public static readonly DateTimeOffset Now = new(2026, 9, 27, 18, 30, 0, TimeSpan.Zero);

    public static NotificationDispatchService Dispatcher(
        INotificationChannelRepository channels,
        HttpMessageHandler handler,
        UnitSystem units = UnitSystem.Metric,
        TimeProvider? time = null,
        ILogger<NotificationDispatchService>? logger = null)
    {
        var clock = time ?? new FixedTime(Now);
        return new NotificationDispatchService(channels, new FixedSettings(units), All(handler), new HeartbeatScheduleService(clock), clock, logger ?? NullLogger<NotificationDispatchService>.Instance);
    }

    public static IReadOnlyList<INotificationTypeService> All(HttpMessageHandler handler) =>
        [.. new ServiceCollection()
            .AddSingleton<IHttpClientFactory>(new StubHttpClientFactory(handler))
            .AddNotifications()
            .BuildServiceProvider()
            .GetServices<INotificationTypeService>()];
}
