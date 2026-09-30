using System.Security.Claims;
using EBikeManager.Web.Resources;
using EBikeManager.Web.Services;
using EBikeManager.Web.Utils;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace EBikeManager.Web.Configuration;

public sealed partial class OidcOptionsSetup : IConfigureNamedOptions<OpenIdConnectOptions>
{
    private readonly AuthSettingsService _auth;
    private readonly ILogger<OidcOptionsSetup> _logger;

    public OidcOptionsSetup(AuthSettingsService auth, ILogger<OidcOptionsSetup> logger)
    {
        _auth = auth;
        _logger = logger;
    }

    public void Configure(string? name, OpenIdConnectOptions options)
    {
        if (name != AuthSettingsService.OidcScheme) return;

        var current = _auth.Current;
        options.Authority = current.SignIn.Authority;
        options.ClientId = current.SignIn.ClientId;
        options.ClientSecret = current.ClientSecret;
        options.RequireHttpsMetadata = current.SignIn.Authority?.StartsWith("https://", StringComparison.OrdinalIgnoreCase) == true;

        options.Scope.Clear();
        foreach (var scope in current.SignIn.ScopeList) options.Scope.Add(scope);

        options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.ResponseType = OpenIdConnectResponseType.Code;
        options.ResponseMode = OpenIdConnectResponseMode.Query;
        options.UsePkce = true;
        options.CorrelationCookie.SameSite = SameSiteMode.Lax;
        options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.NonceCookie.SameSite = SameSiteMode.Lax;
        options.NonceCookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;

        options.GetClaimsFromUserInfoEndpoint = true;
        options.MapInboundClaims = false;
        options.SaveTokens = false;
        options.TokenValidationParameters.NameClaimType = SignedInUser.NameClaim;

        options.Events.OnTokenValidated = context =>
        {
            if (context.Principal?.Identity is ClaimsIdentity identity) identity.AddClaim(new Claim(SignInCookie.StampClaim, _auth.Stamp ?? ""));
            return Task.CompletedTask;
        };
        options.Events.OnRemoteFailure = context =>
        {
            LogSignInFailed(context.Failure);
            context.Response.Redirect($"/auth/failed?reason={Uri.EscapeDataString(context.Failure?.Message ?? WebStrings.AuthFailedReason)}");
            context.HandleResponse();
            return Task.CompletedTask;
        };
    }

    public void Configure(OpenIdConnectOptions options) => Configure(Options.DefaultName, options);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Signing in with the OpenID Connect provider failed")]
    private partial void LogSignInFailed(Exception? exception);
}
