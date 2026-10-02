using EBikeManager.Application.Services;
using EBikeManager.Application.Services.Interfaces;
using EBikeManager.Application.Services.Notifications;
using EBikeManager.Application.Services.Notifications.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace EBikeManager.Application.Installers;

public static class NotificationInstaller
{
    public static IServiceCollection AddNotifications(this IServiceCollection services)
    {
        services.AddScoped<INotificationTypeService, DiscordNotificationService>();
        services.AddScoped<INotificationTypeService, TelegramNotificationService>();
        services.AddScoped<INotificationTypeService, GotifyNotificationService>();
        services.AddScoped<INotificationTypeService, NtfyNotificationService>();
        services.AddScoped<INotificationTypeService, PushoverNotificationService>();
        services.AddScoped<INotificationTypeService, AppriseNotificationService>();
        services.AddScoped<INotificationTypeService, WebhookNotificationService>();
        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<HeartbeatScheduleService>();
        services.AddScoped<INotificationDispatchService, NotificationDispatchService>();
        return services;
    }
}
