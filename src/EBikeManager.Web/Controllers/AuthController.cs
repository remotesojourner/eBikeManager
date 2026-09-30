using EBikeManager.Web.Services;
using EBikeManager.Web.Utils;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EBikeManager.Web.Controllers;

[AllowAnonymous]
[Route("auth")]
public sealed class AuthController : ControllerBase
{
    private readonly SignInTicketService _tickets;
    private readonly AuthSettingsService _auth;

    public AuthController(SignInTicketService tickets, AuthSettingsService auth)
    {
        _tickets = tickets;
        _auth = auth;
    }

    [HttpGet("complete")]
    public async Task<IActionResult> CompleteAsync([FromQuery] string ticket, [FromQuery] string? returnUrl)
    {
        if (!_tickets.TryRedeem(ticket)) return LocalRedirect("/");

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            SignInCookie.CreatePrincipal(_auth.Stamp),
            new AuthenticationProperties { IsPersistent = true });
        return LocalRedirect(AccessPolicy.LocalReturnUrl(returnUrl));
    }

    [HttpPost("logout")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LogoutAsync()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return LocalRedirect("/");
    }
}
