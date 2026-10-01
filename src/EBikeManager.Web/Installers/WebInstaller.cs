using EBikeManager.Application.Configuration;
using EBikeManager.Application.Services.Interfaces;
using EBikeManager.Web.Authorization;
using EBikeManager.Web.Configuration;
using EBikeManager.Web.Services;
using EBikeManager.Web.Utils;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.DataProtection.Repositories;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Options;
using MudBlazor.Services;

namespace EBikeManager.Web.Installers;

public static class WebInstaller
{
    public static IServiceCollection AddHosting(this IServiceCollection services)
    {
        services.AddSingleton<IConfigureOptions<EBikeManagerOptions>, EBikeManagerOptionsSetup>();

        services.AddDataProtection().SetApplicationName("EBikeManager");
        services.AddOptions<KeyManagementOptions>()
            .PostConfigure<IOptions<EBikeManagerOptions>, ILoggerFactory>((keyManagement, options, loggerFactory) =>
            {
                var keysDir = new DirectoryInfo(options.Value.KeysDirectory);
                keysDir.Create();
                keyManagement.XmlRepository = new FileSystemXmlRepository(keysDir, loggerFactory);
            });
        services.AddSingleton<ISecretProtectionService, DataProtectionSecretService>();

        services.AddHealthChecks();

        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost;
            options.KnownIPNetworks.Clear();
            options.KnownProxies.Clear();
        });

        return services;
    }

    public static IServiceCollection AddAccessControl(this IServiceCollection services)
    {
        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.Cookie.Name = SignInCookie.Name;
                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                options.ExpireTimeSpan = TimeSpan.FromDays(30);
                options.SlidingExpiration = true;
                options.LoginPath = AccessPolicy.SignInPath;
                options.Events.OnValidatePrincipal = SignInCookie.ValidateAsync;
            })
            .AddOpenIdConnect(AuthSettingsService.OidcScheme, _ => { });
        services.AddSingleton<IConfigureOptions<OpenIdConnectOptions>, OidcOptionsSetup>();

        services.AddSingleton<AuthSettingsService>();
        services.AddSingleton<ISignInStateService>(provider => provider.GetRequiredService<AuthSettingsService>());
        services.AddHttpContextAccessor();
        services.AddScoped<HttpCurrentAccessService>();
        services.AddScoped<CircuitAccessService>();
        services.AddScoped<ICurrentAccessService, CurrentAccessService>();

        services.AddAuthorization(options =>
        {
            options.FallbackPolicy = new AuthorizationPolicyBuilder().AddRequirements(new FullAccessRequirement()).Build();
            options.AddPolicy(AccessPolicies.Statistics, policy => policy.AddRequirements(new StatisticsAccessRequirement()));
        });
        services.AddSingleton<IAuthorizationHandler, FullAccessRequirementHandler>();
        services.AddSingleton<IAuthorizationHandler, StatisticsAccessRequirementHandler>();
        services.AddSingleton<IAuthorizationMiddlewareResultHandler, AccessDeniedResponder>();
        services.AddCascadingAuthenticationState();
        services.AddControllers();
        return services;
    }

    public static IServiceCollection AddWebUi(this IServiceCollection services)
    {
        services.AddRazorComponents().AddInteractiveServerComponents();
        services.AddMudServices();

        services.AddScoped<BrowserInteropService>();
        services.AddScoped<MapViewService>();
        services.AddScoped<PreferencesService>();
        services.AddScoped<SettingsStateService>();
        services.AddScoped<SyncStatusStateService>();
        services.AddScoped<LiveUpdatesService>();
        return services;
    }
}
