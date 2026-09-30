using EBikeManager.Application.Utils;
using EBikeManager.Web.Resources;
using EBikeManager.Web.Services;
using EBikeManager.Web.Utils;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EBikeManager.Web.Controllers;

[AllowAnonymous]
[Route("auth")]
public sealed class AuthController : ControllerBase
{
    private readonly AuthSettingsService _auth;
    private readonly IAntiforgery _antiforgery;

    public AuthController(AuthSettingsService auth, IAntiforgery antiforgery)
    {
        _auth = auth;
        _antiforgery = antiforgery;
    }

    [HttpGet("login")]
    public IActionResult Login([FromQuery] string? returnUrl)
    {
        var target = AccessPolicy.LocalReturnUrl(returnUrl);
        if (!_auth.IsActive || AccessPolicy.StampMatches(_auth.IsActive, User, _auth.Stamp)) return LocalRedirect(target);

        return Challenge(new AuthenticationProperties { RedirectUri = target }, AuthSettingsService.OidcScheme);
    }

    [HttpPost("logout")]
    public async Task<IActionResult> LogoutAsync()
    {
        if (!await _antiforgery.IsRequestValidAsync(HttpContext)) return BadRequest();

        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return LocalRedirect(_auth.IsActive ? "/auth/signed-out" : "/");
    }

    [HttpGet("signed-out")]
    public ContentResult SignedOut() => Page(
        WebStrings.AuthSignedOutTitle,
        WebStrings.Format(WebStrings.AuthSignedOutMessage, ProjectInfo.Name),
        null,
        WebStrings.AuthSignInAgain);

    [HttpGet("failed")]
    public ContentResult Failed([FromQuery] string? reason) => Page(
        WebStrings.AuthFailedTitle,
        string.IsNullOrWhiteSpace(reason) ? WebStrings.AuthFailedReason : reason,
        WebStrings.Format(WebStrings.AuthFailedNote, ProjectInfo.Name, "DISABLE_AUTH=true"),
        WebStrings.TryAgain);

    private static ContentResult Page(string title, string message, string? note, string actionText) => new()
    {
        Content = AuthPage.Render(title, message, note, AccessPolicy.SignInPath, actionText),
        ContentType = "text/html; charset=utf-8"
    };
}
