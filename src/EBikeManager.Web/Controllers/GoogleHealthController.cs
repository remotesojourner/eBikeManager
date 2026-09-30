using EBikeManager.Application.Enums;
using EBikeManager.Application.Services;
using EBikeManager.Web.Utils;
using Microsoft.AspNetCore.Mvc;

namespace EBikeManager.Web.Controllers;

[Route("integrations/google-health")]
public sealed class GoogleHealthController : ControllerBase
{
    private readonly GoogleHealthAccountService _google;

    public GoogleHealthController(GoogleHealthAccountService google)
    {
        _google = google;
    }

    [HttpGet("callback")]
    public async Task<IActionResult> CallbackAsync([FromQuery] string? code, [FromQuery] string? state, [FromQuery] string? error, CancellationToken cancellationToken)
    {
        var result = await _google.CompleteSignInAsync(code, state, error, cancellationToken);
        if (result.Outcome == OperationOutcome.Denied) return Unauthorized();

        return LocalRedirect(GoogleHealthCallback.ResultPage(result.Succeeded ? GoogleHealthCallback.Connected : GoogleHealthCallback.Failed));
    }
}
