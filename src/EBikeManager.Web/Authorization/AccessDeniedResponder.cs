using EBikeManager.Web.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Diagnostics;

namespace EBikeManager.Web.Authorization;

public sealed class AccessDeniedResponder : IAuthorizationMiddlewareResultHandler
{
    public async Task HandleAsync(RequestDelegate next, HttpContext context, AuthorizationPolicy policy, PolicyAuthorizationResult authorizeResult)
    {
        if (authorizeResult.Succeeded)
        {
            await next(context);
            return;
        }

        var request = context.Request;
        if (AccessPolicy.AnswersWithStatus(request.Path))
        {
            if (context.Features.Get<IStatusCodePagesFeature>() is { } statusCodePages) statusCodePages.Enabled = false;
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        context.Response.Redirect(AccessPolicy.SignInPathFor(request.Path + request.QueryString));
    }
}
